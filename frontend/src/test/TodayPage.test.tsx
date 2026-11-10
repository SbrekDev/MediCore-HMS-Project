import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { apiClient } from '../api/axios';
import { routes } from '../routes';
import { sessionStore } from '../auth/session';
import { buildJwt } from './jwt';
import { renderWithProviders } from './renderWithProviders';
import type { DashboardSummaryDto, AppointmentListItemDto } from '../features/today/types';

function signIn(role: 'Admin' | 'Receptionist' | 'Professional'): void {
  sessionStore.set({
    accessToken: buildJwt({ sub: 'u-1', role }),
    userId: 'u-1',
    email: `${role.toLowerCase()}@ceibos.demo`,
    roles: [role],
  });
}

function todayLocalString(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

function summaryFixture(overrides: Partial<DashboardSummaryDto> = {}): DashboardSummaryDto {
  return {
    from: `${todayLocalString()}T00:00:00-03:00`,
    to: `${todayLocalString()}T23:59:59-03:00`,
    appointments: {
      total: 6,
      confirmed: 3,
      completed: 2,
      noShow: 1,
      cancelled: 0,
      sobreturnos: 1,
    },
    patients: { total: 128, addedInRange: 3 },
    professionals: { total: 9, withTemplate: 7 },
    ...overrides,
  };
}

function appointmentFixture(overrides: Partial<AppointmentListItemDto> = {}): AppointmentListItemDto {
  return {
    id: 'appt-1',
    patientId: 'pat-1',
    patientName: 'Juan Perez',
    professionalId: 'prof-1',
    specialtyId: 'spec-1',
    start: `${todayLocalString()}T09:00:00-03:00`,
    durationMinutes: 30,
    status: 'Confirmed',
    isSobreturno: false,
    isLateCancellation: false,
    ...overrides,
  };
}

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  renderWithProviders(<RouterProvider router={router} />);
  return router;
}

describe('TodayPage', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    sessionStore.clear();
  });

  it('renders the counts from a mocked summary', async () => {
    signIn('Admin');
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/dashboard/summary') {
        return Promise.resolve({ data: summaryFixture() });
      }
      if (url === '/appointments') {
        return Promise.resolve({ data: [] });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    renderAt('/today');

    expect(await screen.findByText('6')).toBeInTheDocument();
    expect(screen.getByText('128')).toBeInTheDocument();
    expect(screen.getByText('9')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('requests the summary range derived from the local day with offset-bearing from and to', async () => {
    signIn('Admin');
    const get = vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/dashboard/summary') {
        return Promise.resolve({ data: summaryFixture() });
      }
      if (url === '/appointments') {
        return Promise.resolve({ data: [] });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    renderAt('/today');

    await waitFor(() =>
      expect(get).toHaveBeenCalledWith(
        '/dashboard/summary',
        expect.objectContaining({
          params: expect.objectContaining({
            from: expect.stringMatching(
              /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$/
            ),
            to: expect.stringMatching(
              /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$/
            ),
          }),
        })
      )
    );
  });

  it('renders the day appointments from the existing list endpoint', async () => {
    signIn('Admin');
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/dashboard/summary') {
        return Promise.resolve({ data: summaryFixture() });
      }
      if (url === '/appointments') {
        return Promise.resolve({ data: [appointmentFixture()] });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    renderAt('/today');

    expect(await screen.findByText(/juan perez/i)).toBeInTheDocument();
    expect(screen.getByText(/09:00/i)).toBeInTheDocument();
  });

  it('shows a visible error when the summary call fails', async () => {
    signIn('Admin');
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/dashboard/summary') {
        return Promise.reject(new Error('Network error'));
      }
      if (url === '/appointments') {
        return Promise.resolve({ data: [] });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    renderAt('/today');

    expect(await screen.findByRole('alert')).toBeInTheDocument();
  });

  it('does not render a figure the payload lacks', async () => {
    signIn('Admin');
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/dashboard/summary') {
        return Promise.resolve({
          data: summaryFixture({
            appointments: { total: 6, confirmed: 3, completed: 2, noShow: 1, cancelled: 0, sobreturnos: 0 },
          }),
        });
      }
      if (url === '/appointments') {
        return Promise.resolve({ data: [] });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    renderAt('/today');

    expect(await screen.findByText('6')).toBeInTheDocument();
    // Sobreturnos value is 0, which is in the payload, so it may be rendered.
    // This test primarily guards against inventing a cap or other non-payload figure.
    expect(screen.queryByText(/cap/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/\/\s*2/i)).not.toBeInTheDocument();
  });
});
