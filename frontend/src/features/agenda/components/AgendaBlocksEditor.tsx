import { useEffect } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Button,
  Card,
  CardHeader,
  EmptyState,
  Field,
  Input,
  Select,
} from '../../../components/ui';
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
    <div className="flex flex-col gap-5">
      <Card>
        <CardHeader title="Sobreturno cap" />
        <div className="p-5">
          <p className="text-[13px] text-muted">
            This is the maximum number of extra appointments the professional can
            accept in a single day.
          </p>
          <div className="mt-4 max-w-[200px]">
            <Field
              label="Max sobreturnos per day"
              htmlFor="maxSobreturnosPerDay"
              error={errors.maxSobreturnosPerDay?.message}
            >
              <Input
                id="maxSobreturnosPerDay"
                type="number"
                min={0}
                {...register('maxSobreturnosPerDay', { valueAsNumber: true })}
              />
            </Field>
          </div>
        </div>
      </Card>

      <form
        onSubmit={handleSubmit((values) => onSave(toUpdateAgendaRequest(values)))}
        noValidate
      >
        <Card>
          <CardHeader
            title="Weekly availability"
            actions={
              <Button
                type="submit"
                variant="primary"
                disabled={isSaving}
              >
                Save agenda
              </Button>
            }
          />
          <div className="p-5">
            {serverError && (
              <div className="mb-5">
                <Alert>{serverError}</Alert>
              </div>
            )}

            {fields.length === 0 && (
              <EmptyState>No blocks configured yet.</EmptyState>
            )}

            {fields.map((field, index) => (
              <div
                key={field.id}
                className="grid grid-cols-1 items-start gap-4 border-b border-line pb-4 last:border-b-0 last:pb-0 sm:grid-cols-[1.25fr_1fr_1fr_auto] sm:items-end"
              >
                <Field
                  label={`Day ${index + 1}`}
                  htmlFor={`block-day-${index}`}
                  error={errors.blocks?.[index]?.dayOfWeek?.message}
                >
                  <Select
                    id={`block-day-${index}`}
                    {...register(`blocks.${index}.dayOfWeek`)}
                  >
                    {DAYS_OF_WEEK.map((day) => (
                      <option key={day} value={day}>
                        {day}
                      </option>
                    ))}
                  </Select>
                </Field>

                <Field
                  label={`Start ${index + 1}`}
                  htmlFor={`block-start-${index}`}
                  error={errors.blocks?.[index]?.start?.message}
                >
                  <Input
                    id={`block-start-${index}`}
                    type="time"
                    {...register(`blocks.${index}.start`)}
                  />
                </Field>

                <Field
                  label={`End ${index + 1}`}
                  htmlFor={`block-end-${index}`}
                  error={errors.blocks?.[index]?.end?.message}
                >
                  <Input
                    id={`block-end-${index}`}
                    type="time"
                    {...register(`blocks.${index}.end`)}
                  />
                </Field>

                <Button
                  type="button"
                  variant="danger"
                  size="sm"
                  onClick={() => remove(index)}
                >
                  Remove block {index + 1}
                </Button>
              </div>
            ))}

            <div className="mt-5 flex items-center gap-2">
              <Button
                type="button"
                variant="secondary"
                onClick={() => append({ ...NEW_BLOCK })}
              >
                Add block
              </Button>
            </div>
          </div>
        </Card>
      </form>
    </div>
  );
}
