import type { AxiosError } from 'axios';

export function describeBookingError(error: unknown, fallback: string): string {
  const status = (error as AxiosError | undefined)?.response?.status;
  const code = (error as AxiosError<{ code: string }> | undefined)?.response?.data?.code;

  if (status === 409 && code === 'Appointment.ProfessionalOverlap') {
    return 'The professional already has an appointment overlapping the requested time range.';
  }

  if (status === 409 && code === 'Appointment.PatientOverlap') {
    return 'The patient already has an appointment overlapping the requested time range.';
  }

  if (status === 400 && code === 'Appointment.SpecialtyNotHeld') {
    return 'The professional does not hold the requested specialty.';
  }

  if (status === 403 && code === 'Appointment.SobreturnoNotAuthorized') {
    return 'You are not authorized to book a sobreturno.';
  }

  if (status === 409 && code === 'Appointment.SobreturnoCapExceeded') {
    return 'The daily sobreturno cap has been reached.';
  }

  if (status === 409 && code === 'Appointment.SobreturnoWithinNormalCapacity') {
    return 'A normal slot is still available; a sobreturno is not needed.';
  }

  if (status === 403) {
    return 'You do not have permission to perform this action.';
  }

  if (status === 404) {
    return 'The requested appointment was not found.';
  }

  if (status === 409 && code === 'Appointment.InvalidState') {
    return 'The appointment is no longer in a state that allows this action.';
  }

  return fallback;
}
