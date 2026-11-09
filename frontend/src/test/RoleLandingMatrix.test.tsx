import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { cleanup, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { routes } from '../routes';
import { sessionStore } from '../auth/session';
import { buildJwt } from './jwt';
import { renderWithProviders } from './renderWithProviders';

type Role = 'Admin' | 'Receptionist' | 'Professional';

/** Everything each account is allowed to open, guards included. */
const OPENABLE: Record<Role, string[]> = {
  Admin: ['/', '/patients', '/booking', '/agenda', '/professionals', '/my-appointments'],
  Receptionist: ['/', '/patients', '/booking', '/my-appointments'],
  Professional: ['/', '/my-appointments'],
};

const LABELS = ['Front desk', 'Clinical', 'Administration'];

function signIn(role: Role, professionalId?: string): void {
  const payload: Record<string, unknown> = { sub: 'u-1', role };
  if (professionalId) payload.professional_id = professionalId;
  sessionStore.set({
    accessToken: buildJwt(payload),
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

function nav() {
  return screen.getByRole('navigation', { name: 'Main' });
}

function navHrefs(): string[] {
  return within(nav())
    .queryAllByRole('link')
    .map((a) => a.getAttribute('href') ?? '');
}

const ACCOUNTS: Array<[string, Role, string | undefined, string]> = [
  ['Admin', 'Admin', undefined, '/patients'],
  ['Receptionist', 'Receptionist', undefined, '/patients'],
  ['Professional with claim', 'Professional', 'p-1', '/my-appointments'],
  ['Professional without claim', 'Professional', undefined, '/my-appointments'],
];

describe('role landing matrix', () => {
  beforeEach(() => sessionStore.clear());
  afterEach(() => sessionStore.clear());

  it('sends an anonymous visitor to /login', () => {
    const router = createMemoryRouter(routes, { initialEntries: ['/'] });
    renderWithProviders(<RouterProvider router={router} />);

    expect(router.state.location.pathname).toBe('/login');
  });

  it.each(ACCOUNTS)('%s lands on a page it can open', (_label, role, claim, expected) => {
    signIn(role, claim);
    const router = renderAt('/');

    expect(router.state.location.pathname).toBe(expected);
    expect(screen.queryByText(/not authorized to view this page/i)).toBeNull();
  });

  it.each(ACCOUNTS)(
    '%s only sees links that resolve inside its own pages',
    async (_label, role, claim) => {
      const user = userEvent.setup();
      signIn(role, claim);
      renderAt('/agenda');
      const hrefs = [...new Set(navHrefs())];
      cleanup();

      for (const href of hrefs) {
        signIn(role, claim);
        const router = renderAt('/agenda');
        const link = within(nav())
          .queryAllByRole('link')
          .find((a) => (a.getAttribute('href') ?? '') === href);

        expect(link, `missing nav link ${href}`).toBeTruthy();
        await user.click(link!);

        expect(OPENABLE[role]).toContain(router.state.location.pathname);
        expect(screen.queryByText(/not authorized to view this page/i)).toBeNull();
        cleanup();
      }
    }
  );

  it.each<[string, Role, string | undefined, string[]]>([
    ['Admin', 'Admin', undefined, ['Front desk', 'Clinical', 'Administration']],
    ['Receptionist', 'Receptionist', undefined, ['Front desk']],
    ['Professional with claim', 'Professional', 'p-1', ['Clinical']],
    ['Professional without claim', 'Professional', undefined, []],
  ])('%s sees no empty navigation group', (_label, role, claim, expected) => {
    signIn(role, claim);
    renderAt('/agenda');

    expect(LABELS.filter((label) => within(nav()).queryByText(label) !== null)).toEqual(
      expected
    );
  });
});
