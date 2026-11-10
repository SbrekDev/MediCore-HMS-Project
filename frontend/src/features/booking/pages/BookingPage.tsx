import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Badge,
  Button,
  Card,
  CardHeader,
  Checkbox,
  Field,
  Input,
  PageHeader,
  Select,
} from '../../../components/ui';
import { getProfessionals, getSpecialties } from '../../professionals/api';
import { searchPatients } from '../../patients/api';
import {
  bookingFormSchema,
  describeWindow,
  toCreateAppointmentRequest,
  toDayRangeStrings,
  type BookingFormData,
} from '../components/bookingSchema';
import { AppointmentList } from '../components/AppointmentList';
import {
  cancelAppointment,
  completeAppointment,
  createAppointment,
  getAvailability,
  listAppointments,
  noShowAppointment,
  rescheduleAppointment,
} from '../api';
import { describeBookingError } from '../errors';

export function BookingPage() {
  const queryClient = useQueryClient();
  const [serverError, setServerError] = useState<string | null>(null);
  const [reschedulingId, setReschedulingId] = useState<string | null>(null);

  const { data: professionals = [] } = useQuery({
    queryKey: ['professionals'],
    queryFn: getProfessionals,
  });

  const { data: patients = [] } = useQuery({
    queryKey: ['patients'],
    queryFn: () => searchPatients(''),
  });

  const { data: specialties = [] } = useQuery({
    queryKey: ['specialties'],
    queryFn: getSpecialties,
  });

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<BookingFormData>({
    resolver: zodResolver(bookingFormSchema),
    defaultValues: {
      patientId: '',
      professionalId: '',
      specialtyId: '',
      date: '',
      time: '',
      isSobreturno: false,
    },
  });

  const professionalId = watch('professionalId');
  const date = watch('date');

  const { data: appointments = [], isLoading: isLoadingAppointments } = useQuery({
    queryKey: ['appointments'],
    queryFn: () => {
      if (date === '' || professionalId === '') {
        return Promise.resolve([]);
      }
      const range = toDayRangeStrings(date);
      return listAppointments({
        from: range.from,
        to: range.to,
        professionalId,
      });
    },
  });

  useEffect(() => {
    if (date !== '' && professionalId !== '') {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
    }
  }, [date, professionalId, queryClient]);

  const { data: availability, isLoading: isLoadingAvailability } = useQuery({
    queryKey: ['availability', professionalId, date],
    queryFn: () => getAvailability(professionalId, date),
    enabled: professionalId !== '' && date !== '',
  });

  function invalidateDay(): void {
    queryClient.invalidateQueries({
      queryKey: ['appointments'],
    });
    queryClient.invalidateQueries({
      queryKey: ['availability', professionalId, date],
    });
  }

  const book = useMutation({
    mutationFn: createAppointment,
    onSuccess: () => {
      setServerError(null);
      invalidateDay();
    },
    onError: (error) => {
      setServerError(
        describeBookingError(
          error,
          'Unable to book the appointment. Please try again.'
        )
      );
    },
  });

  const reschedule = useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: { start: string };
    }) => rescheduleAppointment(id, request),
    onSuccess: () => {
      setServerError(null);
      setReschedulingId(null);
      invalidateDay();
    },
    onError: (error) => {
      setServerError(
        describeBookingError(
          error,
          'Unable to reschedule the appointment. Please try again.'
        )
      );
    },
  });

  const cancel = useMutation({
    mutationFn: cancelAppointment,
    onSuccess: () => {
      setServerError(null);
      invalidateDay();
    },
    onError: (error) => {
      setServerError(
        describeBookingError(
          error,
          'Unable to cancel the appointment. Please try again.'
        )
      );
    },
  });

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

  return (
    <div data-testid="booking-page" className="flex flex-col gap-5">
      <PageHeader
        title="Booking"
        description="Book and manage appointments for a professional."
      />

      {serverError && (
        <Alert>{serverError}</Alert>
      )}

      <Card>
        <CardHeader title="Book appointment" />
        <form
          onSubmit={handleSubmit((values) =>
            book.mutate(toCreateAppointmentRequest(values))
          )}
          noValidate
          className="p-5"
        >
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <Field
              label="Professional"
              htmlFor="booking-professional"
              error={errors.professionalId?.message}
            >
              <Select id="booking-professional" {...register('professionalId')}>
                <option value="">Select a professional</option>
                {professionals.map((professional) => (
                  <option key={professional.id} value={professional.id}>
                    {professional.firstName} {professional.lastName}
                  </option>
                ))}
              </Select>
            </Field>

            <Field
              label="Patient"
              htmlFor="booking-patient"
              error={errors.patientId?.message}
            >
              <Select id="booking-patient" {...register('patientId')}>
                <option value="">Select a patient</option>
                {patients.map((patient) => (
                  <option key={patient.id} value={patient.id}>
                    {patient.firstName} {patient.lastName}
                  </option>
                ))}
              </Select>
            </Field>

            <Field
              label="Specialty"
              htmlFor="booking-specialty"
              error={errors.specialtyId?.message}
            >
              <Select id="booking-specialty" {...register('specialtyId')}>
                <option value="">Select a specialty</option>
                {specialties.map((specialty) => (
                  <option key={specialty.id} value={specialty.id}>
                    {specialty.name}
                  </option>
                ))}
              </Select>
            </Field>

            <Field
              label="Date"
              htmlFor="booking-date"
              error={errors.date?.message}
            >
              <Input id="booking-date" type="date" {...register('date')} />
            </Field>

            <Field
              label="Time"
              htmlFor="booking-time"
              error={errors.time?.message}
            >
              <Input id="booking-time" type="time" {...register('time')} />
            </Field>

            <div className="flex items-end pb-2">
              <Checkbox
                id="booking-sobreturno"
                label="Sobreturno"
                {...register('isSobreturno')}
              />
            </div>
          </div>

          <div className="mt-5">
            <Button type="submit" variant="primary" disabled={book.isPending}>
              Book appointment
            </Button>
          </div>
        </form>
      </Card>

      <Card>
        <CardHeader title="Availability" />
        <div className="p-5">
          {isLoadingAvailability && (
            <p className="text-[13px] text-muted-2">Loading availability…</p>
          )}

          {availability && availability.windows.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {availability.windows.map((window, index) => (
                <Badge key={index} tone="brand">
                  {describeWindow(window)}
                </Badge>
              ))}
            </div>
          )}

          {availability && availability.windows.length === 0 && (
            <p className="text-[13px] text-muted">No availability for this day.</p>
          )}
        </div>
      </Card>

      {isLoadingAppointments && (
        <p className="text-[13px] text-muted-2">Loading appointments…</p>
      )}

      <AppointmentList
        appointments={appointments}
        patients={patients}
        reschedulingId={reschedulingId}
        onRescheduleClick={(id) => setReschedulingId(id)}
        onRescheduleSubmit={(id, request) => reschedule.mutate({ id, request })}
        onCancel={(id) => cancel.mutate(id)}
        onComplete={(id) => complete.mutate(id)}
        onNoShow={(id) => noShow.mutate(id)}
      />
    </div>
  );
}
