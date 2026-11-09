import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { apiClient } from '../api/axios';
import { routes } from '../routes';
import { sessionStore } from '../auth/session';
import { buildJwt } from './jwt';
import { renderWithProviders } from './renderWithProviders';
import type { MyAppointmentListItemDto } from '../features/my-appointments/types';

function signIn(options: { role: 'Professional' | 'Admin' | 'Receptionist'; professionalId?: string }) {
  const payload: Record<string, unknown> = { sub: 'u-1', role: options.role };
  if (options.professionalId) {
    payload.professional_id = options.professionalId;
  }

  sessionStore.set({
    accessToken: buildJwt(payload),
    userId: 'u-1',
    email: `${options.role.toLowerCase()}@ceibos.demo`,
    roles: [options.role],
  });
}

function todayLocalString(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  renderWithProviders(<RouterProvider router={router} />);
  return router;
}

function appointmentFixture(overrides: Partial<MyAppointmentListItemDto> = {}): MyAppointmentListItemDto {
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

describe('MyAppointmentsPage', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    sessionStore.clear();
  });

  it('shows the explicit not-linked message and makes no appointments call', async () => {
    signIn({ role: 'Professional' });
    const get = vi.spyOn(apiClient, 'get').mockRejectedValue(new Error('Should not be called'));

    renderAt('/my-appointments');

    expect(
      await screen.findByText(/this account is not linked to a professional/i)
    ).toBeInTheDocument();
    expect(get).not.toHaveBeenCalled();
  });

  it('lists the linked professional appointments for today with offset-bearing from and to', async () => {
    signIn({ role: 'Professional', professionalId: 'prof-1' });
    const get = vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/appointments') {
        return Promise.resolve({ data: [appointmentFixture()] });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    renderAt('/my-appointments');

    await waitFor(() =>
      expect(get).toHaveBeenCalledWith(
        '/appointments',
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

    expect(await screen.findByText(/juan perez/i)).toBeInTheDocument();
    expect(screen.getByText(/09:00/i)).toBeInTheDocument();
  });

  it('completes an appointment and refreshes the list', async () => {
    signIn({ role: 'Professional', professionalId: 'prof-1' });
    let current = [appointmentFixture()];
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/appointments') {
        return Promise.resolve({ data: current });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    const post = vi.spyOn(apiClient, 'post').mockImplementation((url: string) => {
      if (url === '/appointments/appt-1/complete') {
        current = [appointmentFixture({ status: 'Completed' })];
        return Promise.resolve({ data: { id: 'appt-1', status: 'Completed' } });
      }
      return Promise.resolve({ data: {} });
    });

    renderAt('/my-appointments');
    const user = userEvent.setup();

    expect(await screen.findByText(/juan perez/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /complete appointment/i }));

    await waitFor(() => expect(post).toHaveBeenCalledWith('/appointments/appt-1/complete'));
    expect(await screen.findByText(/completed/i)).toBeInTheDocument();
  });

  it('marks an appointment as no-show and refreshes the list', async () => {
    signIn({ role: 'Professional', professionalId: 'prof-1' });
    let current = [appointmentFixture()];
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/appointments') {
        return Promise.resolve({ data: current });
      }
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    const post = vi.spyOn(apiClient, 'post').mockImplementation((url: string) => {
      if (url === '/appointments/appt-1/no-show') {
        current = [appointmentFixture({ status: 'NoShow' })];
        return Promise.resolve({ data: { id: 'appt-1', status: 'NoShow' } });
      }
      return Promise.resolve({ data: {} });
    });

    renderAt('/my-appointments');
    const user = userEvent.setup();

    expect(await screen.findByText(/juan perez/i)).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: /mark appointment appt-1 as no-show/i })
    );

    await waitFor(() => expect(post).toHaveBeenCalledWith('/appointments/appt-1/no-show'));
    expect(await screen.findByText(/^No-show$/i)).toBeInTheDocument();
  });

  it('surfaces a 403 API response as a visible message', async () => {
    signIn({ role: 'Professional', professionalId: 'prof-1' });
    vi.spyOn(apiClient, 'get').mockRejectedValue({
      response: {
        status: 403,
        data: {
          code: 'Appointment.NotOwner',
          message: 'You can only view your own appointments.',
        },
      },
    });

    renderAt('/my-appointments');

    expect(await screen.findByRole('alert')).toHaveTextContent(/permission/i);
  });

  it('offers the My appointments navigation link only when the token carries professional_id', async () => {
    signIn({ role: 'Professional', professionalId: 'prof-1' });

    renderAt('/my-appointments');

    expect(await screen.findByRole('link', { name: /my appointments/i })).toBeInTheDocument();
  });

  it('hides the My appointments navigation link when the token has no professional_id', async () => {
    signIn({ role: 'Professional' });

    renderAt('/my-appointments');

    expect(await screen.findByText(/this account is not linked to a professional/i)).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /my appointments/i })).not.toBeInTheDocument();
  });
});
