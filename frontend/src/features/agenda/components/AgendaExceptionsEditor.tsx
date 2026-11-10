import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Button,
  Card,
  CardHeader,
  Checkbox,
  EmptyState,
  Field,
  Input,
  Table,
  Td,
  Th,
} from '../../../components/ui';
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
      <Card>
        <CardHeader title="Date exceptions" />
        <div className="p-5">
          {agenda.exceptions.length === 0 ? (
            <EmptyState>No exceptions for this professional.</EmptyState>
          ) : (
            <Table>
              <thead>
                <tr>
                  <Th>Date</Th>
                  <Th>Window</Th>
                  <Th>Reason</Th>
                  <Th className="text-right">Actions</Th>
                </tr>
              </thead>
              <tbody>
                {agenda.exceptions.map((exception) => (
                  <tr key={exception.id}>
                    <Td>{exception.date}</Td>
                    <Td>{describeExceptionWindow(exception)}</Td>
                    <Td>{exception.reason}</Td>
                    <Td className="text-right">
                      <Button
                        type="button"
                        variant="danger"
                        size="sm"
                        disabled={isDeleting}
                        onClick={() => onDelete(exception.id)}
                      >
                        Delete exception {exception.date}
                      </Button>
                    </Td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}

          <form
            onSubmit={handleSubmit((values) => onAdd(toCreateExceptionRequest(values)))}
            noValidate
            className="mt-6 border-t border-line pt-5"
          >
            {serverError && (
              <div className="mb-5">
                <Alert>{serverError}</Alert>
              </div>
            )}

            <h4 className="mb-4 text-sm font-semibold text-text">New exception</h4>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field
                label="Exception date"
                htmlFor="exception-date"
                error={errors.date?.message}
              >
                <Input id="exception-date" type="date" {...register('date')} />
              </Field>

              <div className="flex items-end pb-2">
                <Checkbox id="exception-full-day" label="Full day" {...register('isFullDay')} />
              </div>

              {!isFullDay && (
                <>
                  <Field
                    label="Exception start"
                    htmlFor="exception-start"
                    error={errors.start?.message}
                  >
                    <Input
                      id="exception-start"
                      type="time"
                      {...register('start')}
                    />
                  </Field>

                  <Field
                    label="Exception end"
                    htmlFor="exception-end"
                    error={errors.end?.message}
                  >
                    <Input id="exception-end" type="time" {...register('end')} />
                  </Field>
                </>
              )}

              <div className="sm:col-span-2">
                <Field
                  label="Reason"
                  htmlFor="exception-reason"
                  error={errors.reason?.message}
                >
                  <Input id="exception-reason" {...register('reason')} />
                </Field>
              </div>
            </div>

            <div className="mt-4 flex items-center gap-2">
              <Button type="submit" variant="primary" disabled={isAdding}>
                Add exception
              </Button>
            </div>
          </form>
        </div>
      </Card>
    </section>
  );
}
