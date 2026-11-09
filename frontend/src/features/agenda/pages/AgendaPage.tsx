import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
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
    <div data-testid="agenda-page">
      <h2>Agenda</h2>

      <div>
        <label htmlFor="agenda-professional">Professional</label>
        <select
          id="agenda-professional"
          value={professionalId}
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
        </select>
      </div>

      {isProfessionalsError && (
        <p role="alert">Unable to load professionals. Please try again.</p>
      )}

      {isLoadingProfessionals && <p>Loading professionals…</p>}

      {professionalId === '' && !isProfessionalsError && (
        <p>Select a professional to edit their agenda.</p>
      )}

      {isLoadingAgenda && <p>Loading agenda…</p>}

      {isAgendaError && <p role="alert">Unable to load the agenda. Please try again.</p>}

      {agenda && (
        <>
          <AgendaBlocksEditor
            agenda={agenda}
            isSaving={save.isPending}
            serverError={saveError}
            onSave={(request) => save.mutate(request)}
          />

          {deleteError && <p role="alert">{deleteError}</p>}

          <AgendaExceptionsEditor
            key={exceptionFormKey}
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
