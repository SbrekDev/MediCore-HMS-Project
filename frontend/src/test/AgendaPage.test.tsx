import { describe, it, expect, vi, afterEach } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { apiClient } from '../api/axios';
import { AgendaPage } from '../features/agenda/pages/AgendaPage';
import { renderWithProviders } from './renderWithProviders';
import type { ProfessionalDto } from '../features/professionals/types';
import type { AgendaDto, AgendaExceptionDto } from '../features/agenda/types';

const professionals: ProfessionalDto[] = [
  {
    id: 'prof-1',
    firstName: 'Laura',
    lastName: 'Gómez',
    nationalLicenseMN: 'MN-12345',
    provincialLicenses: [],
  },
];

const exception: AgendaExceptionDto = {
  id: 'exc-1',
  date: '2026-12-25',
  start: null,
  end: null,
  reason: 'Christmas',
};

function agendaFixture(overrides: Partial<AgendaDto> = {}): AgendaDto {
  return {
    professionalId: 'prof-1',
    maxSobreturnosPerDay: 2,
    blocks: [
      { dayOfWeek: 'Monday', start: '08:00', end: '12:00' },
      { dayOfWeek: 'Monday', start: '14:00', end: '18:00' },
    ],
    exceptions: [],
    ...overrides,
  };
}

/** Serves the professionals list and the agenda, so a refetch sees the latest fixture. */
function mockGet(agenda: () => AgendaDto) {
  return vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
    if (url === '/professionals') return Promise.resolve({ data: professionals });
    if (url === '/professionals/prof-1/agenda') return Promise.resolve({ data: agenda() });
    return Promise.reject(new Error(`Unexpected GET ${url}`));
  });
}

async function openAgendaForLaura(): Promise<void> {
  renderWithProviders(<AgendaPage />);
  const user = userEvent.setup();
  await screen.findByRole('option', { name: /laura gómez/i });
  await user.selectOptions(screen.getByLabelText(/professional/i), 'prof-1');
}

describe('AgendaPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads the selected professional template, cap and exceptions from the API', async () => {
    mockGet(() => agendaFixture({ exceptions: [exception] }));

    await openAgendaForLaura();

    expect(await screen.findByLabelText(/start 1/i)).toHaveValue('08:00');
    expect(screen.getByLabelText(/end 1/i)).toHaveValue('12:00');
    expect(screen.getByLabelText(/start 2/i)).toHaveValue('14:00');
    expect(screen.getByLabelText(/max sobreturnos per day/i)).toHaveValue(2);
    expect(screen.getByText('Christmas')).toBeInTheDocument();
  });

  it('sends the weekly template to the PUT with the wire strings the API accepts', async () => {
    mockGet(() => agendaFixture());
    const put = vi.spyOn(apiClient, 'put').mockResolvedValue({ data: undefined });

    await openAgendaForLaura();

    fireEvent.change(await screen.findByLabelText(/max sobreturnos per day/i), {
      target: { value: '3' },
    });
    await userEvent.setup().click(screen.getByRole('button', { name: /save agenda/i }));

    await waitFor(() => expect(put).toHaveBeenCalledTimes(1));
    expect(put).toHaveBeenCalledWith('/professionals/prof-1/agenda', {
      maxSobreturnosPerDay: 3,
      blocks: [
        { dayOfWeek: 'Monday', start: '08:00', end: '12:00' },
        { dayOfWeek: 'Monday', start: '14:00', end: '18:00' },
      ],
    });
  });

  it('rejects a block whose end is not after its start without calling the API', async () => {
    mockGet(() => agendaFixture());
    const put = vi.spyOn(apiClient, 'put');

    await openAgendaForLaura();

    fireEvent.change(await screen.findByLabelText(/start 1/i), { target: { value: '10:00' } });
    fireEvent.change(screen.getByLabelText(/end 1/i), { target: { value: '09:00' } });
    await userEvent.setup().click(screen.getByRole('button', { name: /save agenda/i }));

    expect(await screen.findByText(/end time must be after the start time/i)).toBeInTheDocument();
    expect(put).not.toHaveBeenCalled();
  });

  it('adds a full-day exception through the POST and shows it in the list', async () => {
    let current = agendaFixture();
    mockGet(() => current);
    const post = vi.spyOn(apiClient, 'post').mockImplementation((_url: string, body: unknown) => {
      current = agendaFixture({ exceptions: [{ ...exception, ...(body as object) }] });
      return Promise.resolve({ data: 'exc-1' });
    });

    await openAgendaForLaura();

    fireEvent.change(await screen.findByLabelText(/exception date/i), {
      target: { value: '2026-12-25' },
    });
    await userEvent.setup().click(screen.getByLabelText(/full day/i));
    await userEvent.setup().type(screen.getByLabelText(/reason/i), 'Christmas');
    await userEvent.setup().click(screen.getByRole('button', { name: /add exception/i }));

    await waitFor(() => expect(post).toHaveBeenCalledTimes(1));
    expect(post).toHaveBeenCalledWith('/professionals/prof-1/agenda/exceptions', {
      date: '2026-12-25',
      start: null,
      end: null,
      reason: 'Christmas',
    });
    expect(await screen.findByText('Christmas')).toBeInTheDocument();
  });

  it('sends the start and end of a partial-day exception', async () => {
    mockGet(() => agendaFixture());
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: 'exc-2' });

    await openAgendaForLaura();

    fireEvent.change(await screen.findByLabelText(/exception date/i), {
      target: { value: '2026-12-31' },
    });
    fireEvent.change(screen.getByLabelText(/exception start/i), {
      target: { value: '08:00' },
    });
    fireEvent.change(screen.getByLabelText(/exception end/i), {
      target: { value: '12:00' },
    });
    await userEvent.setup().type(screen.getByLabelText(/reason/i), 'Training');
    await userEvent.setup().click(screen.getByRole('button', { name: /add exception/i }));

    await waitFor(() => expect(post).toHaveBeenCalledTimes(1));
    expect(post).toHaveBeenCalledWith('/professionals/prof-1/agenda/exceptions', {
      date: '2026-12-31',
      start: '08:00',
      end: '12:00',
      reason: 'Training',
    });
  });

  it('deletes an exception using the id returned by the GET', async () => {
    mockGet(() => agendaFixture({ exceptions: [exception] }));
    const remove = vi.spyOn(apiClient, 'delete').mockResolvedValue({ data: undefined });

    await openAgendaForLaura();

    await userEvent
      .setup()
      .click(await screen.findByRole('button', { name: /delete exception 2026-12-25/i }));

    await waitFor(() => expect(remove).toHaveBeenCalledTimes(1));
    expect(remove).toHaveBeenCalledWith('/professionals/prof-1/agenda/exceptions/exc-1');
  });

  it('surfaces a 403 from the API instead of failing silently', async () => {
    mockGet(() => agendaFixture());
    vi.spyOn(apiClient, 'put').mockRejectedValue({
      response: { status: 403, data: { code: 'Forbidden', message: 'Forbidden' } },
    });

    await openAgendaForLaura();

    await userEvent
      .setup()
      .click(await screen.findByRole('button', { name: /save agenda/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/do not have permission/i);
  });

  it('renders an empty editor for a professional with no agenda yet', async () => {
    mockGet(() => agendaFixture({ maxSobreturnosPerDay: 0, blocks: [], exceptions: [] }));

    await openAgendaForLaura();

    expect(await screen.findByText(/no blocks configured yet/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/max sobreturnos per day/i)).toHaveValue(0);
    expect(screen.getByText(/no exceptions for this professional/i)).toBeInTheDocument();
  });
});
