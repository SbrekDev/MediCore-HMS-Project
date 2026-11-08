import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { routes } from './routes';
import { sessionStore } from './auth/session';
import { buildJwt } from './test/jwt';

function signIn(role: 'Admin' | 'Receptionist' | 'Professional') {
  const accessToken = buildJwt({ sub: 'u-1', role });
  sessionStore.set({ accessToken, userId: 'u-1', email: `${role.toLowerCase()}@ceibos.demo`, roles: [role] });
}

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  );
  return router;
}

describe('App routing', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  it('renders the MediCore HMS layout and the patients page for reception staff', () => {
    signIn('Receptionist');

    renderAt('/patients');

    expect(screen.getByRole('heading', { name: /MediCore HMS/i })).toBeInTheDocument();
    expect(screen.getByTestId('patients-page')).toHaveTextContent('Patients');
  });

  it('redirects the root path to the patients page for reception staff', () => {
    signIn('Receptionist');

    renderAt('/');

    expect(screen.getByTestId('patients-page')).toBeInTheDocument();
  });

  it('sends an anonymous visitor to the login page', async () => {
    const router = renderAt('/patients');

    expect(await screen.findByRole('button', { name: /sign in/i })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/login');
    expect(router.state.location.state).toEqual({ from: '/patients' });
  });
});
