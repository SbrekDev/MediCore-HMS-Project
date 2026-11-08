import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { apiClient } from '../api/axios';
import { PatientsPage } from '../features/patients/pages/PatientsPage';
import { renderWithProviders } from './renderWithProviders';
import type { PatientDto } from '../features/patients/types';

const adultPatient: PatientDto = {
  id: 'p-1',
  documentType: 'DNI',
  documentNumber: '30111222',
  firstName: 'Juan',
  lastName: 'Perez',
  birthDate: '1985-06-15',
  contact: { mobilePhone: '1112223333' },
  address: { street: 'Av Siempre Viva', number: '742', city: 'Springfield', province: 'BA', postalCode: '1000' },
  consentPending: false,
};

const minorPatient: PatientDto = {
  id: 'p-2',
  documentType: 'DNI',
  documentNumber: '50111222',
  firstName: 'Lucia',
  lastName: 'Perez',
  birthDate: '2015-03-10',
  contact: { mobilePhone: '1112224444' },
  address: { street: 'Av Siempre Viva', number: '742', city: 'Springfield', province: 'BA', postalCode: '1000' },
  consentPending: true,
};

describe('PatientsPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('search sends the search param and renders returned patients', async () => {
    const get = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [adultPatient, minorPatient] });

    renderWithProviders(<PatientsPage />);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/search/i), 'Perez');
    await user.click(screen.getByRole('button', { name: /search/i }));

    await waitFor(() =>
      expect(get).toHaveBeenCalledWith(
        '/patients',
        expect.objectContaining({ params: { search: 'Perez' } })
      )
    );

    expect(screen.getByText('DNI 30111222')).toBeInTheDocument();
    expect(screen.getByText(/Juan Perez/i)).toBeInTheDocument();
    expect(screen.getByText(/Lucia Perez/i)).toBeInTheDocument();
    expect(screen.getByText(/consent pending/i)).toBeInTheDocument();
  });

  it('blocks submit when required fields are missing and does not call the API', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] });
    const post = vi.spyOn(apiClient, 'post');

    renderWithProviders(<PatientsPage />);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: /new patient/i }));
    await user.click(screen.getByRole('button', { name: /register patient/i }));

    expect((await screen.findAllByText(/required/i)).length).toBeGreaterThan(0);
    expect(post).not.toHaveBeenCalled();
  });

  it('rejects a minor without a responsible client-side', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] });
    const post = vi.spyOn(apiClient, 'post');

    renderWithProviders(<PatientsPage />);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: /new patient/i }));

    await user.selectOptions(screen.getByLabelText(/document type/i), 'DNI');
    await user.type(screen.getByLabelText(/document number/i), '50111222');
    await user.type(screen.getByLabelText(/first name/i), 'Lucia');
    await user.type(screen.getByLabelText(/last name/i), 'Perez');
    await user.type(screen.getByLabelText(/birth date/i), '2015-03-10');
    await user.type(screen.getByLabelText(/mobile phone/i), '1112224444');
    await user.type(screen.getByLabelText(/street name/i), 'Av Siempre Viva');
    await user.type(screen.getByLabelText(/street number/i), '742');
    await user.type(screen.getByLabelText(/city/i), 'Springfield');
    await user.type(screen.getByLabelText(/province/i), 'BA');
    await user.type(screen.getByLabelText(/postal code/i), '1000');

    await user.click(screen.getByRole('button', { name: /register patient/i }));

    expect(await screen.findByText(/responsible patient is required for minors/i)).toBeInTheDocument();
    expect(post).not.toHaveBeenCalled();
  });

  it('shows the duplicate-document message on 409 and keeps the form values', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] });
    const post = vi.spyOn(apiClient, 'post').mockRejectedValue({
      response: { status: 409, data: { code: 'Patient.DuplicateDocument' } },
    });

    renderWithProviders(<PatientsPage />);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: /new patient/i }));

    await user.selectOptions(screen.getByLabelText(/document type/i), 'DNI');
    await user.type(screen.getByLabelText(/document number/i), '30111222');
    await user.type(screen.getByLabelText(/first name/i), 'Ana');
    await user.type(screen.getByLabelText(/last name/i), 'Garcia');
    await user.type(screen.getByLabelText(/birth date/i), '1990-05-10');
    await user.type(screen.getByLabelText(/mobile phone/i), '1234567890');
    await user.type(screen.getByLabelText(/street name/i), 'Calle Falsa');
    await user.type(screen.getByLabelText(/street number/i), '123');
    await user.type(screen.getByLabelText(/city/i), 'Ciudad');
    await user.type(screen.getByLabelText(/province/i), 'X');
    await user.type(screen.getByLabelText(/postal code/i), '2000');

    await user.click(screen.getByRole('button', { name: /register patient/i }));

    expect(await screen.findByText(/already exists/i)).toBeInTheDocument();
    expect(post).toHaveBeenCalledTimes(1);
    expect(screen.getByDisplayValue('Ana')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Garcia')).toBeInTheDocument();
  });

  it('successful create posts the exact body and refreshes the list', async () => {
    const get = vi
      .spyOn(apiClient, 'get')
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({ data: [adultPatient] });
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: { patientId: 'p-1' } });

    renderWithProviders(<PatientsPage />);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: /new patient/i }));

    await user.selectOptions(screen.getByLabelText(/document type/i), 'DNI');
    await user.type(screen.getByLabelText(/document number/i), '30111222');
    await user.type(screen.getByLabelText(/first name/i), 'Juan');
    await user.type(screen.getByLabelText(/last name/i), 'Perez');
    await user.type(screen.getByLabelText(/birth date/i), '1985-06-15');
    await user.type(screen.getByLabelText(/mobile phone/i), '1112223333');
    await user.type(screen.getByLabelText(/street name/i), 'Av Siempre Viva');
    await user.type(screen.getByLabelText(/street number/i), '742');
    await user.type(screen.getByLabelText(/city/i), 'Springfield');
    await user.type(screen.getByLabelText(/province/i), 'BA');
    await user.type(screen.getByLabelText(/postal code/i), '1000');

    await user.click(screen.getByRole('button', { name: /register patient/i }));

    await waitFor(() => expect(post).toHaveBeenCalled());

    expect(post).toHaveBeenCalledWith('/patients', {
      documentType: 'DNI',
      documentNumber: '30111222',
      firstName: 'Juan',
      lastName: 'Perez',
      birthDate: '1985-06-15',
      contact: { mobilePhone: '1112223333', landlinePhone: null, email: null },
      address: { street: 'Av Siempre Viva', number: '742', city: 'Springfield', province: 'BA', postalCode: '1000' },
      responsiblePatientId: null,
    });

    await waitFor(() => expect(get).toHaveBeenCalledTimes(2));
    expect(screen.getByText(/Juan Perez/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /register patient/i })).not.toBeInTheDocument();
  });
  it('shows an error instead of the empty state when the search fails', async () => {
    vi.spyOn(apiClient, 'get').mockRejectedValue({ response: { status: 500 } });

    renderWithProviders(<PatientsPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent(/unable to load patients/i);
    expect(screen.queryByText(/no patients found/i)).not.toBeInTheDocument();
  });
});
