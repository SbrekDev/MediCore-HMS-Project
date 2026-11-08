import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider, type InitialEntry } from 'react-router';
import { LoginPage } from './LoginPage';
import { apiClient } from '../../../api/axios';
import { sessionStore } from '../../../auth/session';
import { buildJwt } from '../../../test/jwt';

function renderLogin(initialEntries: InitialEntry[] = ['/login']) {
  const routes = [
    { path: '/login', element: <LoginPage /> },
    { path: '/patients', element: <div data-testid="patients-page">Patients</div> },
    { path: '/professionals', element: <div data-testid="professionals-page">Professionals</div> },
  ];
  const router = createMemoryRouter(routes, { initialEntries });
  render(<RouterProvider router={router} />);
  return router;
}

describe('LoginPage', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('submits the entered credentials and stores the access token', async () => {
    const accessToken = buildJwt({ sub: 'u-1', role: 'Receptionist' });
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({
      data: { userId: 'u-1', email: 'receptionist@ceibos.demo', accessToken },
    });

    const router = renderLogin();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/email/i), 'receptionist@ceibos.demo');
    await user.type(screen.getByLabelText(/password/i), 'TestPass123!');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(sessionStore.get()?.accessToken).toBe(accessToken);
    });

    expect(post).toHaveBeenCalledWith(
      '/auth/login',
      { email: 'receptionist@ceibos.demo', password: 'TestPass123!' },
      expect.objectContaining({ withCredentials: true })
    );
    expect(sessionStore.get()?.roles).toEqual(['Receptionist']);
    await waitFor(() => expect(router.state.location.pathname).toBe('/patients'));
  });

  it('shows an error and stores nothing when credentials are rejected', async () => {
    vi.spyOn(apiClient, 'post').mockRejectedValue({
      response: { status: 401 },
    });

    renderLogin();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/email/i), 'wrong@ceibos.demo');
    await user.type(screen.getByLabelText(/password/i), 'bad-password');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/invalid email or password/i);
    expect(sessionStore.get()).toBeNull();
  });

  it('returns to the originally requested route after login', async () => {
    const accessToken = buildJwt({ sub: 'u-1', role: 'Admin' });
    vi.spyOn(apiClient, 'post').mockResolvedValue({
      data: { userId: 'u-1', email: 'admin@ceibos.demo', accessToken },
    });

    const entry: InitialEntry = { pathname: '/login', state: { from: '/professionals' } };
    const router = renderLogin([entry]);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/email/i), 'admin@ceibos.demo');
    await user.type(screen.getByLabelText(/password/i), 'TestPass123!');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/professionals'));
  });
});
