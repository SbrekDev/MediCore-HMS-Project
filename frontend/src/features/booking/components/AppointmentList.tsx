import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  pad,
  rescheduleFormSchema,
  toRescheduleAppointmentRequest,
  type RescheduleFormData,
} from './bookingSchema';
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

  function statusLabel(status: string): string {
    return status === 'NoShow' ? 'No-show' : status;
  }

  return (
    <div>
      <h3>Appointments</h3>
      {appointments.length === 0 ? (
        <p>No appointments for this day.</p>
      ) : (
        <ul>
          {appointments.map((appointment) => (
            <li key={appointment.id}>
              <div>
                <span>{patientName(appointment.patientId)}</span>
                {' — '}
                <span>{formatStartTime(appointment.start)}</span>
                {' — '}
                <span>{statusLabel(appointment.status)}</span>
                {appointment.isLateCancellation && (
                  <span>{' — '}Late cancellation</span>
                )}
              </div>

              {reschedulingId === appointment.id ? (
                <form
                  onSubmit={handleSubmit((values) =>
                    onRescheduleSubmit(
                      appointment.id,
                      toRescheduleAppointmentRequest(values)
                    )
                  )}
                  noValidate
                >
                  <div>
                    <label htmlFor={`reschedule-date-${appointment.id}`}>
                      Reschedule date
                    </label>
                    <input
                      id={`reschedule-date-${appointment.id}`}
                      type="date"
                      {...register('date')}
                    />
                  </div>

                  <div>
                    <label htmlFor={`reschedule-time-${appointment.id}`}>
                      Reschedule time
                    </label>
                    <input
                      id={`reschedule-time-${appointment.id}`}
                      type="time"
                      {...register('time')}
                    />
                    {errors.time && <span>{errors.time.message}</span>}
                  </div>

                  <button type="submit">Confirm reschedule</button>
                </form>
              ) : (
                <div>
                  <button
                    type="button"
                    aria-label={`Reschedule appointment ${appointment.id}`}
                    onClick={() => onRescheduleClick(appointment.id)}
                  >
                    Reschedule
                  </button>
                  <button
                    type="button"
                    aria-label={`Cancel appointment ${appointment.id}`}
                    onClick={() => onCancel(appointment.id)}
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    aria-label={`Complete appointment ${appointment.id}`}
                    onClick={() => onComplete(appointment.id)}
                  >
                    Complete
                  </button>
                  <button
                    type="button"
                    aria-label={`No-show appointment ${appointment.id}`}
                    onClick={() => onNoShow(appointment.id)}
                  >
                    Mark no-show
                  </button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
