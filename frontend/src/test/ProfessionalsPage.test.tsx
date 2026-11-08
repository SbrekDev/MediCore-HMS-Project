import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { apiClient } from '../api/axios';
import { ProfessionalsPage } from '../features/professionals/pages/ProfessionalsPage';
import { renderWithProviders } from './renderWithProviders';
import type { ProfessionalDto, SpecialtyDto } from '../features/professionals/types';

const specialties: SpecialtyDto[] = [
  { id: 's-1', code: 'CARDIO', name: 'Cardiología', defaultDurationMinutes: 30, isActive: true },
  { id: 's-2', code: 'CLINICA', name: 'Clínica Médica', defaultDurationMinutes: 30, isActive: true },
];

const professionals: ProfessionalDto[] = [
  {
    id: 'prof-1',
    firstName: 'Laura',
    lastName: 'Gómez',
    nationalLicenseMN: 'MN-12345',
    provincialLicenses: [
      { id: 'mp-1', number: 'MP-111', province: 'BA' },
      { id: 'mp-2', number: 'MP-222', province: 'CABA' },
    ],
  },
  {
    id: 'prof-2',
    firstName: 'Roberto',
    lastName: 'Fernández',
    nationalLicenseMN: 'MN-99999',
    provincialLicenses: [],
  },
];

describe('ProfessionalsPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the list with MN and MP count', async () => {
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: professionals });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });

    renderWithProviders(<ProfessionalsPage />);

    expect(await screen.findByText(/Laura Gómez/i)).toBeInTheDocument();
    expect(screen.getByText('MN-12345')).toBeInTheDocument();
    expect(screen.getByText(/2 MPs/i)).toBeInTheDocument();
    expect(screen.getByText(/Roberto Fernández/i)).toBeInTheDocument();
    expect(screen.getByText('MN-99999')).toBeInTheDocument();
    expect(screen.getByText(/0 MPs/i)).toBeInTheDocument();
  });

  it('requires MN, first name and last name before submitting', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] });
    const post = vi.spyOn(apiClient, 'post');

    renderWithProviders(<ProfessionalsPage />);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: /new professional/i }));

    // Only fill first name to prove the remaining required fields block submission.
    await user.type(screen.getByLabelText(/first name/i), 'Laura');
    await user.click(screen.getByRole('button', { name: /register professional/i }));

    expect(await screen.findAllByText(/required/i)).toHaveLength(2);
    expect(post).not.toHaveBeenCalled();
  });

  it('allows adding provincial licenses and includes them in the POST body', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] });
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: { professionalId: 'prof-3' } });

    renderWithProviders(<ProfessionalsPage />);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: /new professional/i }));

    await user.type(screen.getByLabelText(/first name/i), 'Carlos');
    await user.type(screen.getByLabelText(/last name/i), 'Ruiz');
    await user.type(screen.getByLabelText(/national license mn/i), 'MN-77777');

    await user.click(screen.getByRole('button', { name: /add provincial license/i }));
    await user.type(screen.getByLabelText(/license number 1/i), 'MP-333');
    await user.type(screen.getByLabelText(/license province 1/i), 'Santa Fe');

    await user.click(screen.getByRole('button', { name: /add provincial license/i }));
    await user.type(screen.getByLabelText(/license number 2/i), 'MP-444');
    await user.type(screen.getByLabelText(/license province 2/i), 'Córdoba');

    await user.click(screen.getByRole('button', { name: /register professional/i }));

    await waitFor(() => expect(post).toHaveBeenCalled());

    expect(post).toHaveBeenCalledWith('/professionals', {
      firstName: 'Carlos',
      lastName: 'Ruiz',
      nationalLicenseMN: 'MN-77777',
      provincialLicenses: [
        { number: 'MP-333', province: 'Santa Fe' },
        { number: 'MP-444', province: 'Córdoba' },
      ],
    });
  });

  it('assigns a specialty with isPrimary and durationOverrideMinutes to the correct URL', async () => {
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: professionals });
      if (url === '/specialties') return Promise.resolve({ data: specialties });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });
    const put = vi.spyOn(apiClient, 'put').mockResolvedValue({ data: undefined });

    renderWithProviders(<ProfessionalsPage />);
    const user = userEvent.setup();

    expect(await screen.findByText(/Laura Gómez/i)).toBeInTheDocument();

    await user.click(screen.getAllByRole('button', { name: /assign specialty/i })[0]);

    await user.selectOptions(screen.getByLabelText(/specialty/i), 's-1');
    await user.click(screen.getByLabelText(/primary/i));
    await user.clear(screen.getByLabelText(/duration override/i));
    await user.type(screen.getByLabelText(/duration override/i), '45');

    await user.click(screen.getByRole('button', { name: /save assignment/i }));

    await waitFor(() =>
      expect(put).toHaveBeenCalledWith(
        '/professionals/prof-1/specialties/s-1',
        { isPrimary: true, durationOverrideMinutes: 45 }
      )
    );
  });
  it('shows an error instead of the empty state when the professional list fails', async () => {
    vi.spyOn(apiClient, 'get').mockRejectedValue({ response: { status: 500 } });

    renderWithProviders(<ProfessionalsPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent(/unable to load professionals/i);
    expect(screen.queryByText(/no professionals found/i)).not.toBeInTheDocument();
  });

  it('shows an error when registering a professional fails', async () => {
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: [] });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });
    vi.spyOn(apiClient, 'post').mockRejectedValue({ response: { status: 500 } });

    renderWithProviders(<ProfessionalsPage />);
    const user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: /new professional/i }));
    await user.type(screen.getByLabelText(/^first name$/i), 'Ana');
    await user.type(screen.getByLabelText(/^last name$/i), 'Paz');
    await user.type(screen.getByLabelText(/national license/i), 'MN-777');
    await user.click(screen.getByRole('button', { name: /register professional/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/unable to register the professional/i);
  });

  it('shows an error when the specialty assignment fails', async () => {
    vi.spyOn(apiClient, 'get').mockImplementation((url: string) => {
      if (url === '/professionals') return Promise.resolve({ data: professionals });
      if (url === '/specialties') return Promise.resolve({ data: specialties });
      return Promise.reject(new Error(`Unexpected GET ${url}`));
    });
    vi.spyOn(apiClient, 'put').mockRejectedValue({ response: { status: 500 } });

    renderWithProviders(<ProfessionalsPage />);
    const user = userEvent.setup();

    expect(await screen.findByText(/Laura Gómez/i)).toBeInTheDocument();
    await user.click(screen.getAllByRole('button', { name: /assign specialty/i })[0]);
    await user.selectOptions(screen.getByLabelText(/specialty/i), 's-1');
    await user.click(screen.getByRole('button', { name: /save assignment/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/unable to assign the specialty/i);
  });
});
