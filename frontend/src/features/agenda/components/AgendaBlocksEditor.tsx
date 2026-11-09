import { useEffect } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  agendaFormSchema,
  toUpdateAgendaRequest,
  type AgendaFormData,
} from './agendaSchema';
import { DAYS_OF_WEEK, type AgendaDto, type UpdateAgendaRequest } from '../types';

const NEW_BLOCK = { dayOfWeek: 'Monday', start: '08:00', end: '12:00' } as const;

function toFormValues(agenda: AgendaDto): AgendaFormData {
  return {
    maxSobreturnosPerDay: agenda.maxSobreturnosPerDay,
    blocks: agenda.blocks.map((block) => ({ ...block })),
  };
}

interface AgendaBlocksEditorProps {
  agenda: AgendaDto;
  isSaving: boolean;
  serverError: string | null;
  onSave: (request: UpdateAgendaRequest) => void;
}

export function AgendaBlocksEditor({
  agenda,
  isSaving,
  serverError,
  onSave,
}: AgendaBlocksEditorProps) {
  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AgendaFormData>({
    resolver: zodResolver(agendaFormSchema),
    defaultValues: toFormValues(agenda),
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'blocks' });

  // The template is the server's copy: whenever a save or a refetch replaces it, the
  // editor starts from the persisted blocks instead of keeping unsaved ones.
  useEffect(() => {
    reset(toFormValues(agenda));
  }, [agenda, reset]);

  return (
    <form
      onSubmit={handleSubmit((values) => onSave(toUpdateAgendaRequest(values)))}
      noValidate
    >
      {serverError && <p role="alert">{serverError}</p>}

      <div>
        <label htmlFor="maxSobreturnosPerDay">Max sobreturnos per day</label>
        <input
          id="maxSobreturnosPerDay"
          type="number"
          min={0}
          {...register('maxSobreturnosPerDay', { valueAsNumber: true })}
        />
        {errors.maxSobreturnosPerDay && (
          <span>{errors.maxSobreturnosPerDay.message}</span>
        )}
      </div>

      <h3>Weekly availability</h3>

      {fields.length === 0 && <p>No blocks configured yet.</p>}

      {fields.map((field, index) => (
        <fieldset key={field.id}>
          <legend>Block {index + 1}</legend>

          <label htmlFor={`block-day-${index}`}>Day {index + 1}</label>
          <select id={`block-day-${index}`} {...register(`blocks.${index}.dayOfWeek`)}>
            {DAYS_OF_WEEK.map((day) => (
              <option key={day} value={day}>
                {day}
              </option>
            ))}
          </select>

          <label htmlFor={`block-start-${index}`}>Start {index + 1}</label>
          <input
            id={`block-start-${index}`}
            type="time"
            {...register(`blocks.${index}.start`)}
          />
          {errors.blocks?.[index]?.start && (
            <span>{errors.blocks[index]?.start?.message}</span>
          )}

          <label htmlFor={`block-end-${index}`}>End {index + 1}</label>
          <input id={`block-end-${index}`} type="time" {...register(`blocks.${index}.end`)} />
          {errors.blocks?.[index]?.end && (
            <span>{errors.blocks[index]?.end?.message}</span>
          )}

          <button type="button" onClick={() => remove(index)}>
            Remove block {index + 1}
          </button>
        </fieldset>
      ))}

      <button type="button" onClick={() => append({ ...NEW_BLOCK })}>
        Add block
      </button>
      <button type="submit" disabled={isSaving}>
        Save agenda
      </button>
    </form>
  );
}
