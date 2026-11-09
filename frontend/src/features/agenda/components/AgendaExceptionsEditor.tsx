import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  describeExceptionWindow,
  exceptionFormSchema,
  toCreateExceptionRequest,
  type ExceptionFormData,
} from './agendaSchema';
import type { AgendaDto, CreateAgendaExceptionRequest } from '../types';

interface AgendaExceptionsEditorProps {
  agenda: AgendaDto;
  isAdding: boolean;
  isDeleting: boolean;
  serverError: string | null;
  onAdd: (request: CreateAgendaExceptionRequest) => void;
  onDelete: (exceptionId: string) => void;
}

export function AgendaExceptionsEditor({
  agenda,
  isAdding,
  isDeleting,
  serverError,
  onAdd,
  onDelete,
}: AgendaExceptionsEditorProps) {
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<ExceptionFormData>({
    resolver: zodResolver(exceptionFormSchema),
    defaultValues: {
      date: '',
      isFullDay: false,
      start: '08:00',
      end: '12:00',
      reason: '',
    },
  });

  const isFullDay = useWatch({ control, name: 'isFullDay' });

  return (
    <section>
      <h3>Date exceptions</h3>

      <ul>
        {agenda.exceptions.map((exception) => (
          <li key={exception.id}>
            <span>{exception.date}</span>
            <span>{describeExceptionWindow(exception)}</span>
            <span>{exception.reason}</span>
            <button
              type="button"
              disabled={isDeleting}
              onClick={() => onDelete(exception.id)}
            >
              Delete exception {exception.date}
            </button>
          </li>
        ))}
      </ul>

      {agenda.exceptions.length === 0 && <p>No exceptions for this professional.</p>}

      <form
        onSubmit={handleSubmit((values) => onAdd(toCreateExceptionRequest(values)))}
        noValidate
      >
        {serverError && <p role="alert">{serverError}</p>}

        <label htmlFor="exception-date">Exception date</label>
        <input id="exception-date" type="date" {...register('date')} />
        {errors.date && <span>{errors.date.message}</span>}

        <label htmlFor="exception-full-day">Full day</label>
        <input id="exception-full-day" type="checkbox" {...register('isFullDay')} />

        {!isFullDay && (
          <>
            <label htmlFor="exception-start">Exception start</label>
            <input id="exception-start" type="time" {...register('start')} />
            {errors.start && <span>{errors.start.message}</span>}

            <label htmlFor="exception-end">Exception end</label>
            <input id="exception-end" type="time" {...register('end')} />
            {errors.end && <span>{errors.end.message}</span>}
          </>
        )}

        <label htmlFor="exception-reason">Reason</label>
        <input id="exception-reason" {...register('reason')} />
        {errors.reason && <span>{errors.reason.message}</span>}

        <button type="submit" disabled={isAdding}>
          Add exception
        </button>
      </form>
    </section>
  );
}
