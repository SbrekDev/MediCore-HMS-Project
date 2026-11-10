import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { cleanup, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { routes } from '../routes';
import { sessionStore } from '../auth/session';
import { buildJwt } from './jwt';
import { renderWithProviders } from './renderWithProviders';

type Role = 'Admin' | 'Receptionist' | 'Professional';

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

function navLinks(): HTMLAnchorElement[] {
  return within(screen.getByRole('navigation', { name: 'Main' })).queryAllByRole(
    'link'
  ) as HTMLAnchorElement[];
}

function navHrefs(): string[] {
  return navLinks().map((a) => a.getAttribute('href') ?? '');
}

describe('shell navigation matrix', () => {
  beforeEach(() => sessionStore.clear());
  afterEach(() => sessionStore.clear());

  it.each<[string, Role, string | undefined, string[]]>([
    [
      'Admin',
      'Admin',
      undefined,
      ['/today', '/patients', '/booking', '/agenda', '/professionals'],
    ],
    ['Receptionist', 'Receptionist', undefined, ['/today', '/patients', '/booking']],
    ['Professional without a claim', 'Professional', undefined, ['/today']],
    ['Professional with a claim', 'Professional', 'p-1', ['/today', '/my-appointments']],
  ])(
    'offers %s exactly the links their routes allow',
    (_, role, claim, expected) => {
      signIn(role, claim);
      renderAt('/agenda');

      expect(navHrefs()).toEqual(expected);
    }
  );

  it('hides the Professionals link from a Receptionist but shows Agenda to an Admin', () => {
    signIn('Receptionist');
    renderAt('/patients');
    expect(navHrefs()).not.toContain('/professionals');
    expect(navHrefs()).not.toContain('/agenda');
    cleanup();

    signIn('Admin');
    renderAt('/patients');
    expect(navHrefs()).toEqual(expect.arrayContaining(['/agenda', '/professionals']));
  });
});
