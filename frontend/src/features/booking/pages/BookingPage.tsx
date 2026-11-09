import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
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
    <div data-testid="booking-page">
      <h2>Booking</h2>

      {serverError && <p role="alert">{serverError}</p>}

      <form
        onSubmit={handleSubmit((values) =>
          book.mutate(toCreateAppointmentRequest(values))
        )}
        noValidate
      >
        <div>
          <label htmlFor="booking-professional">Professional</label>
          <select id="booking-professional" {...register('professionalId')}>
            <option value="">Select a professional</option>
            {professionals.map((professional) => (
              <option key={professional.id} value={professional.id}>
                {professional.firstName} {professional.lastName}
              </option>
            ))}
          </select>
          {errors.professionalId && <span>{errors.professionalId.message}</span>}
        </div>

        <div>
          <label htmlFor="booking-patient">Patient</label>
          <select id="booking-patient" {...register('patientId')}>
            <option value="">Select a patient</option>
            {patients.map((patient) => (
              <option key={patient.id} value={patient.id}>
                {patient.firstName} {patient.lastName}
              </option>
            ))}
          </select>
          {errors.patientId && <span>{errors.patientId.message}</span>}
        </div>

        <div>
          <label htmlFor="booking-specialty">Specialty</label>
          <select id="booking-specialty" {...register('specialtyId')}>
            <option value="">Select a specialty</option>
            {specialties.map((specialty) => (
              <option key={specialty.id} value={specialty.id}>
                {specialty.name}
              </option>
            ))}
          </select>
          {errors.specialtyId && <span>{errors.specialtyId.message}</span>}
        </div>

        <div>
          <label htmlFor="booking-date">Date</label>
          <input id="booking-date" type="date" {...register('date')} />
          {errors.date && <span>{errors.date.message}</span>}
        </div>

        <div>
          <label htmlFor="booking-time">Time</label>
          <input id="booking-time" type="time" {...register('time')} />
          {errors.time && <span>{errors.time.message}</span>}
        </div>

        <div>
          <label htmlFor="booking-sobreturno">
            <input
              id="booking-sobreturno"
              type="checkbox"
              {...register('isSobreturno')}
            />
            Sobreturno
          </label>
        </div>

        <button type="submit" disabled={book.isPending}>
          Book appointment
        </button>
      </form>

      {isLoadingAvailability && <p>Loading availability…</p>}

      {availability && availability.windows.length > 0 && (
        <div>
          <h3>Availability</h3>
          <ul>
            {availability.windows.map((window, index) => (
              <li key={index}>{describeWindow(window)}</li>
            ))}
          </ul>
        </div>
      )}

      {availability && availability.windows.length === 0 && (
        <p>No availability for this day.</p>
      )}

      {isLoadingAppointments && <p>Loading appointments…</p>}

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
