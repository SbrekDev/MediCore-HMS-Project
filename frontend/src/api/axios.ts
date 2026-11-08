import axios from 'axios';
import { sessionStore } from '../auth/session';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  // The refresh token travels in an httpOnly cookie scoped to /api/auth.
  withCredentials: true,
});

let refreshPromise: Promise<string> | null = null;

apiClient.interceptors.request.use((config) => {
  const token = sessionStore.get()?.accessToken;
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

      // Single-flight: concurrent 401s share one refresh round trip.
      if (!refreshPromise) {
        refreshPromise = refreshAccessToken();
      }

      try {
        const newToken = await refreshPromise;
        const current = sessionStore.get();
        if (current) {
          sessionStore.set({ ...current, accessToken: newToken });
        }
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        sessionStore.clear();
        return Promise.reject(refreshError);
      } finally {
        refreshPromise = null;
      }
    }

    return Promise.reject(error);
  }
);

async function refreshAccessToken(): Promise<string> {
  // Bare axios: refresh must not recurse through the 401 interceptor above.
  const response = await axios.post('/api/auth/refresh', {}, { withCredentials: true });
  return response.data.accessToken as string;
}

// Extend AxiosRequestConfig for the retry flag.
declare module 'axios' {
  export interface InternalAxiosRequestConfig {
    _retry?: boolean;
  }
}
