import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Card,
  CardHeader,
  Field,
  PageHeader,
  Select,
} from '../../../components/ui';
import { getProfessionals } from '../../professionals/api';
import {
  createAgendaException,
  deleteAgendaException,
  getAgenda,
  updateAgenda,
} from '../api';
import { AgendaBlocksEditor } from '../components/AgendaBlocksEditor';
import { AgendaExceptionsEditor } from '../components/AgendaExceptionsEditor';
import { describeAgendaError } from '../errors';
import type { CreateAgendaExceptionRequest, UpdateAgendaRequest } from '../types';

export function AgendaPage() {
  const queryClient = useQueryClient();
  const [professionalId, setProfessionalId] = useState('');
  const [saveError, setSaveError] = useState<string | null>(null);
  const [exceptionError, setExceptionError] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  // The exception form is remounted after a successful add, so its inputs never keep
  // the values of the exception that was just persisted.
  const [exceptionFormKey, setExceptionFormKey] = useState(0);

  const {
    data: professionals = [],
    isLoading: isLoadingProfessionals,
    isError: isProfessionalsError,
  } = useQuery({ queryKey: ['professionals'], queryFn: getProfessionals });

  const {
    data: agenda,
    isLoading: isLoadingAgenda,
    isError: isAgendaError,
  } = useQuery({
    queryKey: ['agenda', professionalId],
    queryFn: () => getAgenda(professionalId),
    enabled: professionalId !== '',
  });

  function invalidateAgenda(): void {
    queryClient.invalidateQueries({ queryKey: ['agenda', professionalId] });
  }

  const save = useMutation({
    mutationFn: (request: UpdateAgendaRequest) => updateAgenda(professionalId, request),
    onSuccess: () => {
      setSaveError(null);
      invalidateAgenda();
    },
    onError: (error) => {
      setSaveError(describeAgendaError(error, 'Unable to save the agenda. Please try again.'));
    },
  });

  const addException = useMutation({
    mutationFn: (request: CreateAgendaExceptionRequest) =>
      createAgendaException(professionalId, request),
    onSuccess: () => {
      setExceptionError(null);
      setExceptionFormKey((current) => current + 1);
      invalidateAgenda();
    },
    onError: (error) => {
      setExceptionError(
        describeAgendaError(error, 'Unable to add the exception. Please try again.')
      );
    },
  });

  const removeException = useMutation({
    mutationFn: (exceptionId: string) => deleteAgendaException(professionalId, exceptionId),
    onSuccess: () => {
      setDeleteError(null);
      invalidateAgenda();
    },
    onError: (error) => {
      setDeleteError(
        describeAgendaError(error, 'Unable to delete the exception. Please try again.')
      );
    },
  });

  return (
    <div data-testid="agenda-page" className="flex flex-col gap-5">
      <PageHeader
        title="Agenda"
        description="Manage weekly availability and date exceptions."
      />

      <Card>
        <CardHeader title="Professional" />
        <div className="p-5">
          {isProfessionalsError ? (
            <Alert>Unable to load professionals. Please try again.</Alert>
          ) : (
            <div className="max-w-md">
              <Field label="Professional" htmlFor="agenda-professional">
                <Select
                  id="agenda-professional"
                  value={professionalId}
                  disabled={isLoadingProfessionals}
                  onChange={(event) => {
                    setProfessionalId(event.target.value);
                    setSaveError(null);
                    setExceptionError(null);
                    setDeleteError(null);
                  }}
                >
                  <option value="">Select a professional</option>
                  {professionals.map((professional) => (
                    <option key={professional.id} value={professional.id}>
                      {professional.firstName} {professional.lastName}
                    </option>
                  ))}
                </Select>
              </Field>
            </div>
          )}

          {isLoadingProfessionals && (
            <p className="mt-3 text-[13px] text-muted-2">Loading professionals…</p>
          )}

          {professionalId === '' && !isProfessionalsError && !isLoadingProfessionals && (
            <p className="text-[13px] text-muted">
              Select a professional to edit their agenda.
            </p>
          )}
        </div>
      </Card>

      {isLoadingAgenda && (
        <p className="text-[13px] text-muted-2">Loading agenda…</p>
      )}

      {isAgendaError && <Alert>Unable to load the agenda. Please try again.</Alert>}

      {agenda && (
        <>
          <AgendaBlocksEditor
            agenda={agenda}
            isSaving={save.isPending}
            serverError={saveError}
            onSave={(request) => save.mutate(request)}
          />

          {deleteError && <Alert>{deleteError}</Alert>}

          <AgendaExceptionsEditor
            // Keyed by professional as well: returning to an already cached agenda does
            // not unmount the editors, so the form would otherwise keep the previous
            // professional's unsaved date, window and reason.
            key={`${professionalId}:${exceptionFormKey}`}
            agenda={agenda}
            isAdding={addException.isPending}
            isDeleting={removeException.isPending}
            serverError={exceptionError}
            onAdd={(request) => addException.mutate(request)}
            onDelete={(exceptionId) => removeException.mutate(exceptionId)}
          />
        </>
      )}
    </div>
  );
}
