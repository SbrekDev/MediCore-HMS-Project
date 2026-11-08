import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import axios, { AxiosError, type AxiosAdapter } from 'axios';
import { apiClient } from './axios';
import { sessionStore } from '../auth/session';

function unauthorized(config: Parameters<AxiosAdapter>[0]): AxiosError {
  return new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, null, {
    data: {},
    status: 401,
    statusText: 'Unauthorized',
    headers: {},
    config,
  });
}

describe('apiClient 401 handling', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('refreshes once for concurrent 401s and retries each request with the new token', async () => {
    sessionStore.set({
      accessToken: 'expired',
      userId: 'u-1',
      email: 'someone@ceibos.demo',
      roles: ['Admin'],
    });

    const seenAuth: string[] = [];
    const adapter: AxiosAdapter = async (config) => {
      const authorization = String(config.headers.Authorization ?? '');
      seenAuth.push(authorization);
      if (authorization !== 'Bearer fresh-token') {
        throw unauthorized(config);
      }
      return { data: { ok: true }, status: 200, statusText: 'OK', headers: {}, config };
    };
    const originalAdapter = apiClient.defaults.adapter;
    apiClient.defaults.adapter = adapter;

    const refresh = vi
      .spyOn(axios, 'post')
      .mockResolvedValue({ data: { accessToken: 'fresh-token' } });

    try {
      const responses = await Promise.all([
        apiClient.get('/patients'),
        apiClient.get('/professionals'),
        apiClient.get('/agenda'),
      ]);

      expect(responses.every((response) => response.status === 200)).toBe(true);
      expect(refresh).toHaveBeenCalledTimes(1);
      expect(sessionStore.get()?.accessToken).toBe('fresh-token');
      expect(seenAuth.filter((value) => value === 'Bearer fresh-token')).toHaveLength(3);
    } finally {
      apiClient.defaults.adapter = originalAdapter;
    }
  });

  it('clears the session when the refresh itself fails', async () => {
    sessionStore.set({
      accessToken: 'expired',
      userId: 'u-1',
      email: 'someone@ceibos.demo',
      roles: ['Admin'],
    });

    const adapter: AxiosAdapter = async (config) => {
      throw unauthorized(config);
    };
    const originalAdapter = apiClient.defaults.adapter;
    apiClient.defaults.adapter = adapter;

    vi.spyOn(axios, 'post').mockRejectedValue({
      response: { status: 401 },
    });

    try {
      await expect(apiClient.get('/patients')).rejects.toBeTruthy();
      expect(sessionStore.get()).toBeNull();
    } finally {
      apiClient.defaults.adapter = originalAdapter;
    }
  });
});
