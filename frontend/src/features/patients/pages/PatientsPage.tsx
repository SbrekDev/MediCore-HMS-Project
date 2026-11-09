import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createPatient, searchPatients } from '../api';
import { PatientForm } from '../components/PatientForm';
import { PatientList } from '../components/PatientList';

export function PatientsPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [inputValue, setInputValue] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const { data: patients = [], isLoading, isError } = useQuery({
    queryKey: ['patients', search],
    queryFn: () => searchPatients(search),
  });

  const create = useMutation({
    mutationFn: createPatient,
    onSuccess: () => {
      setShowForm(false);
      setServerError(null);
      queryClient.invalidateQueries({ queryKey: ['patients'] });
    },
    onError: (error: unknown) => {
      const axiosError = error as {
        response?: { status?: number; data?: { code?: string } };
      };
      if (
        axiosError.response?.status === 409 &&
        axiosError.response.data?.code === 'Patient.DuplicateDocument'
      ) {
        setServerError('A patient with this document already exists.');
      } else {
        setServerError('Unable to register the patient. Please try again.');
      }
    },
  });

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    setSearch(inputValue);
  }

  function handleSubmit(request: Parameters<typeof createPatient>[0]): void {
    setServerError(null);
    create.mutate(request);
  }

  return (
    <div data-testid="patients-page">
      <h2>Patients</h2>

      <form onSubmit={handleSearch}>
        <label htmlFor="patient-search">Search</label>
        <input
          id="patient-search"
          value={inputValue}
          onChange={(event) => setInputValue(event.target.value)}
          placeholder="Document or name"
        />
        <button type="submit">Search</button>
      </form>

      {!showForm && (
        <button type="button" onClick={() => setShowForm(true)}>
          New patient
        </button>
      )}

      {showForm && (
        <PatientForm
          serverError={serverError}
          onSubmit={handleSubmit}
          onCancel={() => {
            setShowForm(false);
            setServerError(null);
          }}
        />
      )}

      {isError ? (
        <p role="alert">Unable to load patients. Please try again.</p>
      ) : (
        <PatientList patients={patients} isLoading={isLoading} />
      )}
    </div>
  );
}

