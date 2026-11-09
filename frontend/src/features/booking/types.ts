export interface AvailabilityWindowDto {
  start: string;
  end: string;
}

export interface AvailabilityResponse {
  professionalId: string;
  date: string;
  windows: AvailabilityWindowDto[];
  maxSobreturnosPerDay: number;
}

export interface AppointmentListItemDto {
  id: string;
  patientId: string;
  professionalId: string;
  specialtyId: string;
  start: string;
  durationMinutes: number;
  status: string;
  isSobreturno: boolean;
  isLateCancellation: boolean;
}

export interface CreateAppointmentRequest {
  patientId: string;
  professionalId: string;
  specialtyId: string;
  start: string;
  isSobreturno: boolean;
}

export interface RescheduleAppointmentRequest {
  start: string;
}
