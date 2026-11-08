import { apiClient } from '../../api/axios';
import { decodeRoles, sessionStore, type Session } from '../../auth/session';

interface LoginResponse {
  userId: string;
  email: string;
  accessToken: string;
  expiresAt?: string;
}

export async function login(email: string, password: string): Promise<Session> {
  const response = await apiClient.post<LoginResponse>(
    '/auth/login',
    { email, password },
    { withCredentials: true }
  );

  const session: Session = {
    accessToken: response.data.accessToken,
    userId: response.data.userId,
    email: response.data.email,
    roles: decodeRoles(response.data.accessToken),
  };

  sessionStore.set(session);
  return session;
}

export async function logout(): Promise<void> {
  try {
    await apiClient.post('/auth/logout');
  } finally {
    sessionStore.clear();
  }
}
