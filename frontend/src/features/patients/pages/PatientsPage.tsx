import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Button,
  Card,
  CardHeader,
  Field,
  Input,
  PageHeader,
} from '../../../components/ui';
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
    <div data-testid="patients-page" className="flex flex-col gap-5">
      <PageHeader
        title="Patients"
        description="Search and register patients."
        actions={
          !showForm && (
            <Button
              type="button"
              variant="primary"
              onClick={() => setShowForm(true)}
            >
              New patient
            </Button>
          )
        }
      />

      {!showForm ? (
        <Card>
          <CardHeader
            title="Patient directory"
            actions={
              <form onSubmit={handleSearch} className="flex items-end gap-2">
                <div className="min-w-[200px] sm:min-w-[260px]">
                  <Field label="Search" htmlFor="patient-search">
                    <Input
                      id="patient-search"
                      value={inputValue}
                      onChange={(event) => setInputValue(event.target.value)}
                      placeholder="Document or name"
                    />
                  </Field>
                </div>
                <Button type="submit" variant="primary">
                  Search
                </Button>
              </form>
            }
          />
          {isError ? (
            <div className="p-5">
              <Alert>Unable to load patients. Please try again.</Alert>
            </div>
          ) : (
            <PatientList patients={patients} isLoading={isLoading} />
          )}
        </Card>
      ) : (
        <Card>
          <CardHeader
            title="New patient"
            actions={
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  setShowForm(false);
                  setServerError(null);
                }}
              >
                Cancel
              </Button>
            }
          />
          <PatientForm
            serverError={serverError}
            onSubmit={handleSubmit}
            onCancel={() => {
              setShowForm(false);
              setServerError(null);
            }}
          />
        </Card>
      )}
    </div>
  );
}
