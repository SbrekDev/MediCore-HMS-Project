import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { RequireRole } from './RequireRole';
import { sessionStore } from './session';
import { buildJwt } from '../test/jwt';

function renderGuarded(initialPath: string, allowed: string[]) {
  const routes = [
    { path: '/login', element: <div data-testid="login-page">Login</div> },
    {
      path: '/protected',
      element: (
        <RequireRole allowed={allowed}>
          <div data-testid="protected-content">Secret</div>
        </RequireRole>
      ),
    },
  ];

  const router = createMemoryRouter(routes, { initialEntries: [initialPath] });
  render(<RouterProvider router={router} />);
  return router;
}

function signIn(roles: string[]) {
  const accessToken = buildJwt({ sub: 'u-1', role: roles });
  sessionStore.set({ accessToken, userId: 'u-1', email: 'someone@ceibos.demo', roles });
}

describe('RequireRole', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  it('redirects an anonymous visitor to the login page', async () => {
    const router = renderGuarded('/protected', ['Admin']);

    expect(await screen.findByTestId('login-page')).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/login');
  });

  it('remembers the requested route so login can return to it', async () => {
    const router = renderGuarded('/protected', ['Admin']);

    await screen.findByTestId('login-page');
    expect(router.state.location.state).toEqual({ from: '/protected' });
  });

  it('renders the guarded content when the session holds an allowed role', async () => {
    signIn(['Receptionist']);

    renderGuarded('/protected', ['Admin', 'Receptionist']);

    expect(await screen.findByTestId('protected-content')).toBeInTheDocument();
  });

  it('blocks a session whose roles do not include an allowed role', async () => {
    signIn(['Professional']);

    renderGuarded('/protected', ['Admin', 'Receptionist']);

    expect(await screen.findByRole('alert')).toHaveTextContent(/not authorized/i);
    expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
  });
});
