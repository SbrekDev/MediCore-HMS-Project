import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSession, decodeProfessionalId } from '../../../auth/session';
import { toDayRangeStrings, pad } from '../../booking/components/bookingSchema';
import { describeBookingError } from '../../booking/errors';
import {
  completeAppointment,
  listMyAppointments,
  noShowAppointment,
} from '../api';
import type { MyAppointmentListItemDto } from '../types';

function todayLocalString(): string {
  const now = new Date();
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

function formatTime(start: string): string {
  const date = new Date(start);
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function statusLabel(status: string): string {
  return status === 'NoShow' ? 'No-show' : status;
}

export function MyAppointmentsPage() {
  const session = useSession();
  const queryClient = useQueryClient();
  const [serverError, setServerError] = useState<string | null>(null);
  const [date, setDate] = useState<string>(todayLocalString());

  const professionalId = useMemo(() => {
    if (!session) {
      return null;
    }
    return decodeProfessionalId(session.accessToken);
  }, [session]);

  const {
    data: appointments = [],
    isLoading,
    error: listError,
  } = useQuery({
    queryKey: ['my-appointments', professionalId, date],
    queryFn: () => {
      const range = toDayRangeStrings(date);
      return listMyAppointments({ from: range.from, to: range.to });
    },
    enabled: professionalId !== null && date !== '',
  });

  const listErrorMessage = useMemo(() => {
    if (!listError) {
      return null;
    }
    return describeBookingError(
      listError,
      'Unable to load the appointments. Please try again.'
    );
  }, [listError]);

  function invalidateDay(): void {
    queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
  }

  const complete = useMutation({
    mutationFn: completeAppointment,
    onSuccess: () => {
      setServerError(null);
      invalidateDay();
    },
    onError: (error) => {
      setServerError(
        describeBookingError(
          error,
          'Unable to complete the appointment. Please try again.'
        )
      );
    },
  });

  const noShow = useMutation({
    mutationFn: noShowAppointment,
    onSuccess: () => {
      setServerError(null);
      invalidateDay();
    },
    onError: (error) => {
      setServerError(
        describeBookingError(
          error,
          'Unable to mark the appointment as no-show. Please try again.'
        )
      );
    },
  });

  if (professionalId === null) {
    return (
      <div data-testid="my-appointments-page">
        <h2>My appointments</h2>
        <p role="alert">This account is not linked to a professional.</p>
      </div>
    );
  }

  return (
    <div data-testid="my-appointments-page">
      <h2>My appointments</h2>

      {(serverError ?? listErrorMessage) && (
        <p role="alert">{serverError ?? listErrorMessage}</p>
      )}

      <div>
        <label htmlFor="my-appointments-date">Date</label>
        <input
          id="my-appointments-date"
          type="date"
          value={date}
          onChange={(event) => {
            setDate(event.target.value);
            setServerError(null);
          }}
        />
      </div>

      {isLoading && <p>Loading appointments…</p>}

      {appointments.length === 0 ? (
        <p>No appointments for this day.</p>
      ) : (
        <ul>
          {appointments.map((appointment: MyAppointmentListItemDto) => (
            <li key={appointment.id}>
              <div>
                <span>{appointment.patientName}</span>
                {' — '}
                <span>{formatTime(appointment.start)}</span>
                {' — '}
                <span>{statusLabel(appointment.status)}</span>
              </div>
              <div>
                <button
                  type="button"
                  aria-label={`Complete appointment ${appointment.id}`}
                  onClick={() => complete.mutate(appointment.id)}
                >
                  Complete
                </button>
                <button
                  type="button"
                  aria-label={`Mark appointment ${appointment.id} as no-show`}
                  onClick={() => noShow.mutate(appointment.id)}
                >
                  Mark no-show
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
