import axios from 'axios';
import { tokenStore } from '../auth/token';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

let refreshPromise: Promise<string> | null = null;

apiClient.interceptors.request.use((config) => {
  const token = tokenStore.get();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true;

      if (!refreshPromise) {
        refreshPromise = refreshAccessToken();
      }

      try {
        const newToken = await refreshPromise;
        tokenStore.set(newToken);
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        tokenStore.clear();
        return Promise.reject(refreshError);
      } finally {
        refreshPromise = null;
      }
    }

    return Promise.reject(error);
  }
);

async function refreshAccessToken(): Promise<string> {
  // Placeholder for /api/auth/refresh call using the httpOnly refresh cookie.
  // Single-flight is enforced by the shared refreshPromise above.
  const response = await axios.post('/api/auth/refresh', {}, { withCredentials: true });
  return response.data.accessToken as string;
}

// Extend AxiosRequestConfig for the retry flag.
declare module 'axios' {
  export interface InternalAxiosRequestConfig {
    _retry?: boolean;
  }
}
