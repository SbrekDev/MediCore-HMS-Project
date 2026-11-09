import { z } from 'zod';
import type {
  CreateAppointmentRequest,
  RescheduleAppointmentRequest,
} from '../types';

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;

const TWO_HOURS_MS = 2 * 60 * 60 * 1000;
const NINETY_DAYS_MS = 90 * 24 * 60 * 60 * 1000;

export const BOOKING_WINDOW_MESSAGE =
  'Appointments must be between 2 hours and 90 days from now.';

export function pad(n: number): string {
  return String(n).padStart(2, '0');
}

export { DATE_PATTERN, TIME_PATTERN };

/**
 * Formats a local Date as an ISO-8601 instant with an explicit offset,
 * e.g. 2026-12-15T09:00:00-03:00. The server requires the offset and
 * refuses a bound without one.
 */
export function toLocalInstantString(date: Date): string {
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());
  const seconds = pad(date.getSeconds());
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  const absOffset = Math.abs(offset);
  const offsetHours = pad(Math.floor(absOffset / 60));
  const offsetMinutes = pad(absOffset % 60);

  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}${sign}${offsetHours}:${offsetMinutes}`;
}

/**
 * Derives the start-of-day and start-of-next-day instants for a local date
 * string, both with the local explicit offset.
 *
 * The next day is built from calendar components, not by adding 24 hours: in a
 * zone with a DST transition a local day is 23 or 25 hours long, and adding a
 * fixed span would make the range start or end off local midnight.
 */
export function toDayRangeStrings(dateString: string): { from: string; to: string } {
  const [year, month, day] = dateString.split('-').map(Number);
  const start = new Date(year, month - 1, day);
  const end = new Date(year, month - 1, day + 1);
  return { from: toLocalInstantString(start), to: toLocalInstantString(end) };
}

function isWithinBookingWindow(dateString: string, timeString: string): boolean {
  const start = new Date(`${dateString}T${timeString}:00`);
  const now = new Date();
  const min = new Date(now.getTime() + TWO_HOURS_MS);
  const max = new Date(now.getTime() + NINETY_DAYS_MS);
  return start >= min && start <= max;
}

export const bookingFormSchema = z
  .object({
    patientId: z.string().min(1, 'Required'),
    professionalId: z.string().min(1, 'Required'),
    specialtyId: z.string().min(1, 'Required'),
    date: z.string().regex(DATE_PATTERN, 'Required'),
    time: z.string().regex(TIME_PATTERN, 'Required'),
    isSobreturno: z.boolean(),
  })
  .refine((values) => isWithinBookingWindow(values.date, values.time), {
    message: BOOKING_WINDOW_MESSAGE,
    path: ['time'],
  });

export type BookingFormData = z.infer<typeof bookingFormSchema>;

export function toCreateAppointmentRequest(values: BookingFormData): CreateAppointmentRequest {
  const start = new Date(`${values.date}T${values.time}:00`);
  return {
    patientId: values.patientId,
    professionalId: values.professionalId,
    specialtyId: values.specialtyId,
    start: toLocalInstantString(start),
    isSobreturno: values.isSobreturno,
  };
}

export const rescheduleFormSchema = z
  .object({
    date: z.string().regex(DATE_PATTERN, 'Required'),
    time: z.string().regex(TIME_PATTERN, 'Required'),
  })
  .refine((values) => isWithinBookingWindow(values.date, values.time), {
    message: BOOKING_WINDOW_MESSAGE,
    path: ['time'],
  });

export type RescheduleFormData = z.infer<typeof rescheduleFormSchema>;

export function toRescheduleAppointmentRequest(
  values: RescheduleFormData
): RescheduleAppointmentRequest {
  const start = new Date(`${values.date}T${values.time}:00`);
  return { start: toLocalInstantString(start) };
}

export function describeWindow(window: { start: string; end: string }): string {
  return `${formatTime(window.start)}–${formatTime(window.end)}`;
}

function formatTime(value: string): string {
  // The API returns TimeOnly as "HH:mm:ss"; display it as "HH:mm".
  return value.slice(0, 5);
}
