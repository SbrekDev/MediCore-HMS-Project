import { describe, it, expect, vi, afterEach } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { apiClient } from '../api/axios';
import { BookingPage } from '../features/booking/pages/BookingPage';
import { renderWithProviders } from './renderWithProviders';
import type { PatientDto } from '../features/patients/types';
import type { ProfessionalDto, SpecialtyDto } from '../features/professionals/types';
import type {
  AppointmentListItemDto,
  AvailabilityResponse,
} from '../features/booking/types';

const professional: ProfessionalDto = {
  id: 'prof-1',
  firstName: 'Laura',
  lastName: 'Gómez',
  nationalLicenseMN: 'MN-12345',
  provincialLicenses: [],
};

const patient: PatientDto = {
  id: 'pat-1',
  documentType: 'DNI',
  documentNumber: '30111222',
  firstName: 'Juan',
  lastName: 'Perez',
  birthDate: '1985-06-15',
  contact: { mobilePhone: '1112223333' },
  address: {
    street: 'Av Siempre Viva',
    number: '742',
    city: 'Springfield',
    province: 'BA',
    postalCode: '1000',
  },
  consentPending: false,
};

const specialtyCardio: SpecialtyDto = {
  id: 'spec-1',
  code: 'CARD',
  name: 'Cardiología',
  defaultDurationMinutes: 30,
  isActive: true,
};

const specialtyPedia: SpecialtyDto = {
  id: 'spec-2',
  code: 'PED',
  name: 'Pediatría',
  defaultDurationMinutes: 30,
  isActive: true,
};

const specialties: SpecialtyDto[] = [specialtyCardio, specialtyPedia];

const availability: AvailabilityResponse = {
  professionalId: 'prof-1',
  date: '2026-12-15',
  windows: [{ start: '08:00:00', end: '12:00:00' }],
  maxSobreturnosPerDay: 2,
};

function appointmentFixture(
  overrides: Partial<AppointmentListItemDto> = {}
): AppointmentListItemDto {
  return {
    id: 'appt-1',
    patientId: patient.id,
    professionalId: professional.id,
    specialtyId: specialtyCardio.id,
    start: '2026-12-15T09:00:00-03:00',
    durationMinutes: 30,
    status: 'Confirmed',
    isSobreturno: false,
    isLateCancellation: false,
    ...overrides,
  };
}

function pad(n: number): string {
  return String(n).padStart(2, '0');
}

function formatDateInput(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function formatTimeInput(date: Date): string {
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function mockBaseApis(appointments: AppointmentListItemDto[] = []) {
  return vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
    if (url === '/professionals') return Promise.resolve({ data: [professional] });
    if (url === '/patients') return Promise.resolve({ data: [patient] });
    if (url === '/specialties') return Promise.resolve({ data: specialties });
    if (url === '/professionals/prof-1/availability') {
      return Promise.resolve({ data: availability });
    }
    if (url === '/appointments') return Promise.resolve({ data: appointments });
    return Promise.reject(new Error(`Unexpected GET ${url}`));
  });
}

async function renderAndOpenBookingForm(
  appointments: AppointmentListItemDto[] = []
): Promise<ReturnType<typeof userEvent.setup>> {
  mockBaseApis(appointments);
  renderWithProviders(<BookingPage />);
  const user = userEvent.setup();

  await fillBookingForm(user);

  return user;
}

async function fillBookingForm(
  user: ReturnType<typeof userEvent.setup>
): Promise<void> {
  await screen.findByRole('option', { name: /laura gómez/i });
  await user.selectOptions(screen.getByLabelText(/professional/i), 'prof-1');

  await screen.findByRole('option', { name: /juan perez/i });
  await user.selectOptions(screen.getByLabelText(/patient/i), 'pat-1');

  await screen.findByRole('option', { name: /cardiología/i });
  await user.selectOptions(screen.getByLabelText(/specialty/i), 'spec-1');
}

describe('BookingPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads professionals and availability windows for the chosen professional and day', async () => {
    mockBaseApis();
    renderWithProviders(<BookingPage />);
    const user = userEvent.setup();

    await screen.findByRole('option', { name: /laura gómez/i });
    await user.selectOptions(screen.getByLabelText(/professional/i), 'prof-1');

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: '2026-12-15' },
    });

    await waitFor(() =>
      expect(screen.getByText(/08:00–12:00/i)).toBeInTheDocument()
    );
  });

  it('sends POST /api/appointments with the exact contract fields', async () => {
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: { id: 'appt-new' } });
    const user = await renderAndOpenBookingForm();

    const date = new Date();
    date.setDate(date.getDate() + 3);
    date.setHours(9, 0, 0, 0);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: formatDateInput(date) },
    });
    fireEvent.change(screen.getByLabelText(/time/i), {
      target: { value: '09:00' },
    });

    await user.click(screen.getByRole('button', { name: /book appointment/i }));

    await waitFor(() => expect(post).toHaveBeenCalledTimes(1));
    const [url, body] = post.mock.calls[0];
    expect(url).toBe('/appointments');
    expect(body).toMatchObject({
      patientId: 'pat-1',
      professionalId: 'prof-1',
      specialtyId: 'spec-1',
      isSobreturno: false,
    });
    expect((body as { start: string }).start).toMatch(
      /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$/
    );
  });

  it('rejects a start less than 2 hours away with a visible message and no request', async () => {
    const post = vi.spyOn(apiClient, 'post');
    const user = await renderAndOpenBookingForm();

    const tooSoon = new Date();
    tooSoon.setMinutes(tooSoon.getMinutes() + 30);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: formatDateInput(tooSoon) },
    });
    fireEvent.change(screen.getByLabelText(/time/i), {
      target: { value: formatTimeInput(tooSoon) },
    });

    await user.click(screen.getByRole('button', { name: /book appointment/i }));

    expect(
      await screen.findByText(/must be between 2 hours and 90 days from now/i)
    ).toBeInTheDocument();
    expect(post).not.toHaveBeenCalled();
  });

  it('rejects a start more than 90 days ahead with a visible message and no request', async () => {
    const post = vi.spyOn(apiClient, 'post');
    const user = await renderAndOpenBookingForm();

    const tooFar = new Date();
    tooFar.setDate(tooFar.getDate() + 91);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: formatDateInput(tooFar) },
    });
    fireEvent.change(screen.getByLabelText(/time/i), {
      target: { value: '09:00' },
    });

    await user.click(screen.getByRole('button', { name: /book appointment/i }));

    expect(
      await screen.findByText(/must be between 2 hours and 90 days from now/i)
    ).toBeInTheDocument();
    expect(post).not.toHaveBeenCalled();
  });

  it('surfaces the server error when the specialty is not held by the professional', async () => {
    const post = vi.spyOn(apiClient, 'post').mockRejectedValue({
      response: {
        status: 400,
        data: {
          code: 'Appointment.SpecialtyNotHeld',
          message: 'The professional does not hold the requested specialty.',
        },
      },
    });
    const user = await renderAndOpenBookingForm();

    await user.selectOptions(screen.getByLabelText(/specialty/i), 'spec-2');

    const date = new Date();
    date.setDate(date.getDate() + 3);
    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: formatDateInput(date) },
    });
    fireEvent.change(screen.getByLabelText(/time/i), {
      target: { value: '09:00' },
    });

    await user.click(screen.getByRole('button', { name: /book appointment/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/does not hold/i);
    expect(post).toHaveBeenCalledTimes(1);
  });

  it('surfaces a 409 overlap conflict as a visible message', async () => {
    const post = vi.spyOn(apiClient, 'post').mockRejectedValue({
      response: {
        status: 409,
        data: {
          code: 'Appointment.ProfessionalOverlap',
          message: 'The professional already has an appointment overlapping the requested time range.',
        },
      },
    });
    const user = await renderAndOpenBookingForm();

    const date = new Date();
    date.setDate(date.getDate() + 3);
    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: formatDateInput(date) },
    });
    fireEvent.change(screen.getByLabelText(/time/i), {
      target: { value: '09:00' },
    });

    await user.click(screen.getByRole('button', { name: /book appointment/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/already has an appointment/i);
    expect(post).toHaveBeenCalledTimes(1);
  });

  it('discovers the appointment list for the day with offset-bearing from and to', async () => {
    const get = mockBaseApis([appointmentFixture()]);
    renderWithProviders(<BookingPage />);
    const user = userEvent.setup();

    await screen.findByRole('option', { name: /laura gómez/i });
    await user.selectOptions(screen.getByLabelText(/professional/i), 'prof-1');

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: '2026-12-15' },
    });

    await waitFor(() =>
      expect(get).toHaveBeenCalledWith(
        '/appointments',
        expect.objectContaining({
          params: expect.objectContaining({
            professionalId: 'prof-1',
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

    await waitFor(() => expect(screen.getByText(/juan perez/i)).toBeInTheDocument());
    expect(screen.getByText(/09:00/i)).toBeInTheDocument();
  });

  it('reschedules an appointment and refreshes the list', async () => {
    let current = [appointmentFixture()];
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: [professional] });
      if (url === '/patients') return Promise.resolve({ data: [patient] });
      if (url === '/specialties') return Promise.resolve({ data: specialties });
      if (url === '/professionals/prof-1/availability') {
        return Promise.resolve({ data: availability });
      }
      if (url === '/appointments') return Promise.resolve({ data: current });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    const post = vi.spyOn(apiClient, 'post').mockImplementation((url: string) => {
      if (url === '/appointments/appt-1/reschedule') {
        current = [appointmentFixture({ start: '2026-12-15T10:00:00-03:00' })];
        return Promise.resolve({
          data: { id: 'appt-1', start: '2026-12-15T10:00:00-03:00' },
        });
      }
      return Promise.resolve({ data: {} });
    });

    renderWithProviders(<BookingPage />);
    const user = userEvent.setup();
    await fillBookingForm(user);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: '2026-12-15' },
    });

    expect(await screen.findByText(/09:00/i)).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: /reschedule appointment appt-1/i })
    );

    fireEvent.change(screen.getByLabelText(/reschedule date/i), {
      target: { value: '2026-12-15' },
    });
    fireEvent.change(screen.getByLabelText(/reschedule time/i), {
      target: { value: '10:00' },
    });

    await user.click(screen.getByRole('button', { name: /confirm reschedule/i }));

    await waitFor(() =>
      expect(post).toHaveBeenCalledWith(
        '/appointments/appt-1/reschedule',
        expect.objectContaining({
          start: expect.stringMatching(/2026-12-15T10:00:00[+-]\d{2}:\d{2}/),
        })
      )
    );
    await waitFor(() => expect(screen.getByText(/10:00/i)).toBeInTheDocument());
  });

  it('cancels an appointment and refreshes the list', async () => {
    let current = [appointmentFixture()];
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: [professional] });
      if (url === '/patients') return Promise.resolve({ data: [patient] });
      if (url === '/specialties') return Promise.resolve({ data: specialties });
      if (url === '/professionals/prof-1/availability') {
        return Promise.resolve({ data: availability });
      }
      if (url === '/appointments') return Promise.resolve({ data: current });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    const post = vi.spyOn(apiClient, 'post').mockImplementation((url: string) => {
      if (url === '/appointments/appt-1/cancel') {
        current = [];
        return Promise.resolve({
          data: { id: 'appt-1', status: 'Cancelled', isLateCancellation: false },
        });
      }
      return Promise.resolve({ data: {} });
    });

    renderWithProviders(<BookingPage />);
    const user = userEvent.setup();
    await fillBookingForm(user);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: '2026-12-15' },
    });

    expect(await screen.findByText(/09:00/i)).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: /cancel appointment appt-1/i })
    );

    await waitFor(() => expect(post).toHaveBeenCalledWith('/appointments/appt-1/cancel'));
    await waitFor(() => expect(screen.queryByText(/09:00/i)).not.toBeInTheDocument());
  });

  it('completes an appointment and refreshes the list', async () => {
    let current = [appointmentFixture()];
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: [professional] });
      if (url === '/patients') return Promise.resolve({ data: [patient] });
      if (url === '/specialties') return Promise.resolve({ data: specialties });
      if (url === '/professionals/prof-1/availability') {
        return Promise.resolve({ data: availability });
      }
      if (url === '/appointments') return Promise.resolve({ data: current });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    const post = vi.spyOn(apiClient, 'post').mockImplementation((url: string) => {
      if (url === '/appointments/appt-1/complete') {
        current = [appointmentFixture({ status: 'Completed' })];
        return Promise.resolve({ data: { id: 'appt-1', status: 'Completed' } });
      }
      return Promise.resolve({ data: {} });
    });

    renderWithProviders(<BookingPage />);
    const user = userEvent.setup();
    await fillBookingForm(user);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: '2026-12-15' },
    });

    expect(await screen.findByText(/09:00/i)).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: /complete appointment appt-1/i })
    );

    await waitFor(() => expect(post).toHaveBeenCalledWith('/appointments/appt-1/complete'));
    expect(await screen.findByText(/completed/i)).toBeInTheDocument();
  });

  it('marks an appointment as no-show and reflects the outcome in the list without inventing a counter field', async () => {
    let current = [appointmentFixture()];
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: [professional] });
      if (url === '/patients') return Promise.resolve({ data: [patient] });
      if (url === '/specialties') return Promise.resolve({ data: specialties });
      if (url === '/professionals/prof-1/availability') {
        return Promise.resolve({ data: availability });
      }
      if (url === '/appointments') return Promise.resolve({ data: current });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    const post = vi.spyOn(apiClient, 'post').mockImplementation((url: string) => {
      if (url === '/appointments/appt-1/no-show') {
        current = [appointmentFixture({ status: 'NoShow' })];
        return Promise.resolve({ data: { id: 'appt-1', status: 'NoShow' } });
      }
      return Promise.resolve({ data: {} });
    });

    renderWithProviders(<BookingPage />);
    const user = userEvent.setup();
    await fillBookingForm(user);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: '2026-12-15' },
    });

    expect(await screen.findByText(/09:00/i)).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: /no-show appointment appt-1/i })
    );

    await waitFor(() => expect(post).toHaveBeenCalledWith('/appointments/appt-1/no-show'));
    expect(await screen.findByText(/^No-show$/i)).toBeInTheDocument();
    expect(screen.queryByText(/no-show count/i)).not.toBeInTheDocument();
  });

  it('shows the late-cancellation flag returned by the cancel response', async () => {
    let current = [appointmentFixture()];
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: [professional] });
      if (url === '/patients') return Promise.resolve({ data: [patient] });
      if (url === '/specialties') return Promise.resolve({ data: specialties });
      if (url === '/professionals/prof-1/availability') {
        return Promise.resolve({ data: availability });
      }
      if (url === '/appointments') return Promise.resolve({ data: current });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    const post = vi.spyOn(apiClient, 'post').mockImplementation((url: string) => {
      if (url === '/appointments/appt-1/cancel') {
        current = [appointmentFixture({ status: 'Cancelled', isLateCancellation: true })];
        return Promise.resolve({
          data: { id: 'appt-1', status: 'Cancelled', isLateCancellation: true },
        });
      }
      return Promise.resolve({ data: {} });
    });

    renderWithProviders(<BookingPage />);
    const user = userEvent.setup();
    await fillBookingForm(user);

    fireEvent.change(screen.getByLabelText(/date/i), {
      target: { value: '2026-12-15' },
    });

    expect(await screen.findByText(/09:00/i)).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: /cancel appointment appt-1/i })
    );

    await waitFor(() => expect(post).toHaveBeenCalledWith('/appointments/appt-1/cancel'));
    expect(await screen.findByText(/late cancellation/i)).toBeInTheDocument();
  });
});
