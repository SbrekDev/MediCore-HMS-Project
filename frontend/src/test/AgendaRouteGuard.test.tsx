import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { routes } from '../routes';
import { sessionStore } from '../auth/session';
import { buildJwt } from './jwt';
import { renderWithProviders } from './renderWithProviders';

function signIn(role: 'Admin' | 'Professional') {
  const accessToken = buildJwt({ sub: 'u-1', role });
  sessionStore.set({
    accessToken,
    userId: 'u-1',
    email: `${role.toLowerCase()}@ceibos.demo`,
    roles: [role],
  });
}

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  renderWithProviders(<RouterProvider router={router} />);
  return router;
}

describe('/agenda route guard', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  afterEach(() => {
    sessionStore.clear();
  });

  it('renders the agenda page for an Admin session', async () => {
    signIn('Admin');

    renderAt('/agenda');

    expect(await screen.findByTestId('agenda-page')).toHaveTextContent('Agenda');
  });

  it('blocks a Professional session with the unauthorized alert', async () => {
    signIn('Professional');

    renderAt('/agenda');

    expect(await screen.findByRole('alert')).toHaveTextContent(/not authorized/i);
    expect(screen.queryByTestId('agenda-page')).not.toBeInTheDocument();
  });

  it('hides the Agenda navigation link from a Professional session', async () => {
    signIn('Professional');

    renderAt('/agenda');

    expect(await screen.findByRole('link', { name: 'Today' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Agenda' })).not.toBeInTheDocument();
  });

  it('offers the Agenda navigation link to an Admin session', async () => {
    signIn('Admin');

    renderAt('/agenda');

    expect(await screen.findByRole('link', { name: 'Agenda' })).toBeInTheDocument();
  });
});
