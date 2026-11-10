import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSession, decodeProfessionalId } from '../../../auth/session';
import {
  Alert,
  Badge,
  Button,
  Card,
  CardHeader,
  EmptyState,
  Field,
  Input,
  PageHeader,
  Table,
  Td,
  Th,
} from '../../../components/ui';
import { toDayRangeStrings, pad } from '../../booking/components/bookingSchema';
import { statusLabel, statusTone } from '../../../components/appointmentStatus';
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
      <div data-testid="my-appointments-page" className="flex flex-col gap-5">
        <PageHeader title="My appointments" />
        <Alert>This account is not linked to a professional.</Alert>
      </div>
    );
  }

  return (
    <div data-testid="my-appointments-page" className="flex flex-col gap-5">
      <PageHeader
        title="My appointments"
        description="Your appointments for the selected day."
      />

      {(serverError ?? listErrorMessage) && (
        <Alert>{serverError ?? listErrorMessage}</Alert>
      )}

      <Card>
        <CardHeader title="Select date" />
        <div className="p-5">
          <Field label="Date" htmlFor="my-appointments-date">
            <Input
              id="my-appointments-date"
              type="date"
              value={date}
              onChange={(event) => {
                setDate(event.target.value);
                setServerError(null);
              }}
            />
          </Field>
        </div>
      </Card>

      <Card>
        <CardHeader title="Appointments" />
        {isLoading && (
          <p className="px-5 py-8 text-center text-[13px] text-muted-2">
            Loading appointments…
          </p>
        )}

        {!isLoading && appointments.length === 0 && (
          <EmptyState>No appointments for this day.</EmptyState>
        )}

        {!isLoading && appointments.length > 0 && (
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
              {appointments.map((appointment: MyAppointmentListItemDto) => (
                <tr key={appointment.id}>
                  <Td className="font-mono text-xs">
                    {formatTime(appointment.start)}
                  </Td>
                  <Td>{appointment.patientName}</Td>
                  <Td>
                    <Badge tone={statusTone(appointment.status)}>
                      {statusLabel(appointment.status)}
                    </Badge>
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
                        aria-label={`Complete appointment ${appointment.id}`}
                        onClick={() => complete.mutate(appointment.id)}
                      >
                        Complete
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="secondary"
                        aria-label={`Mark appointment ${appointment.id} as no-show`}
                        onClick={() => noShow.mutate(appointment.id)}
                      >
                        Mark no-show
                      </Button>
                    </div>
                  </Td>
                </tr>
              ))}
            </tbody>
          </Table>
        )}
      </Card>
    </div>
  );
}
