import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Badge,
  Button,
  Card,
  CardHeader,
  EmptyState,
  Field,
  Input,
  Table,
  Td,
  Th,
} from '../../../components/ui';
import {
  pad,
  rescheduleFormSchema,
  toRescheduleAppointmentRequest,
  type RescheduleFormData,
} from './bookingSchema';
import { statusLabel, statusTone } from '../../../components/appointmentStatus';
import type { AppointmentListItemDto } from '../types';

interface PatientLookup {
  id: string;
  firstName: string;
  lastName: string;
}

interface AppointmentListProps {
  appointments: AppointmentListItemDto[];
  patients: PatientLookup[];
  reschedulingId: string | null;
  onRescheduleClick: (id: string) => void;
  onRescheduleSubmit: (id: string, request: { start: string }) => void;
  onCancel: (id: string) => void;
  onComplete: (id: string) => void;
  onNoShow: (id: string) => void;
}

export function AppointmentList({
  appointments,
  patients,
  reschedulingId,
  onRescheduleClick,
  onRescheduleSubmit,
  onCancel,
  onComplete,
  onNoShow,
}: AppointmentListProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<RescheduleFormData>({
    resolver: zodResolver(rescheduleFormSchema),
    defaultValues: { date: '', time: '' },
  });

  useEffect(() => {
    reset({ date: '', time: '' });
  }, [reschedulingId, reset]);

  function patientName(patientId: string): string {
    const found = patients.find((p) => p.id === patientId);
    return found ? `${found.firstName} ${found.lastName}` : patientId;
  }

  function formatStartTime(start: string): string {
    const date = new Date(start);
    return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  return (
    <Card>
      <CardHeader title="Appointments" />
      {appointments.length === 0 ? (
        <EmptyState>No appointments for this day.</EmptyState>
      ) : (
        <Table>
          <thead>
            <tr>
              <Th>Time</Th>
              <Th>Patient</Th>
              <Th>Status</Th>
              <Th>Sobreturno</Th>
              <Th className="text-right">Actions</Th>
            </tr>
          </thead>
          <tbody>
            {appointments.map((appointment) =>
              reschedulingId === appointment.id ? (
                <tr key={`${appointment.id}-reschedule`}>
                  <td
                    colSpan={5}
                    className="border-b border-line bg-canvas px-5 py-3 align-middle text-text"
                  >
                    <form
                      onSubmit={handleSubmit((values) =>
                        onRescheduleSubmit(
                          appointment.id,
                          toRescheduleAppointmentRequest(values)
                        )
                      )}
                      noValidate
                      className="flex flex-wrap items-end gap-4"
                    >
                      <div className="min-w-[160px]">
                        <Field
                          label="Reschedule date"
                          htmlFor={`reschedule-date-${appointment.id}`}
                          error={errors.date?.message}
                        >
                          <Input
                            id={`reschedule-date-${appointment.id}`}
                            type="date"
                            {...register('date')}
                          />
                        </Field>
                      </div>
                      <div className="min-w-[140px]">
                        <Field
                          label="Reschedule time"
                          htmlFor={`reschedule-time-${appointment.id}`}
                          error={errors.time?.message}
                        >
                          <Input
                            id={`reschedule-time-${appointment.id}`}
                            type="time"
                            {...register('time')}
                          />
                        </Field>
                      </div>
                      <Button type="submit" variant="primary">
                        Confirm reschedule
                      </Button>
                    </form>
                  </td>
                </tr>
              ) : (
                <tr key={appointment.id}>
                  <Td className="font-mono text-xs">
                    {formatStartTime(appointment.start)}
                  </Td>
                  <Td>{patientName(appointment.patientId)}</Td>
                  <Td>
                    <Badge tone={statusTone(appointment.status)}>
                      {statusLabel(appointment.status)}
                    </Badge>
                    {appointment.isLateCancellation && (
                      <span className="ml-2 text-[11.5px] text-muted-2">
                        Late cancellation
                      </span>
                    )}
                  </Td>
                  <Td>
                    {appointment.isSobreturno && (
                      <Badge tone="warn">Sobreturno</Badge>
                    )}
                  </Td>
                  <Td className="text-right">
                    <div className="flex items-center justify-end gap-2">
                      <Button
                        type="button"
                        size="sm"
                        aria-label={`Reschedule appointment ${appointment.id}`}
                        onClick={() => onRescheduleClick(appointment.id)}
                      >
                        Reschedule
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="danger"
                        aria-label={`Cancel appointment ${appointment.id}`}
                        onClick={() => onCancel(appointment.id)}
                      >
                        Cancel
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="secondary"
                        aria-label={`Complete appointment ${appointment.id}`}
                        onClick={() => onComplete(appointment.id)}
                      >
                        Complete
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="secondary"
                        aria-label={`No-show appointment ${appointment.id}`}
                        onClick={() => onNoShow(appointment.id)}
                      >
                        Mark no-show
                      </Button>
                    </div>
                  </Td>
                </tr>
              )
            )}
          </tbody>
        </Table>
      )}
    </Card>
  );
}
