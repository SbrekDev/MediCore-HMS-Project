export interface MyAppointmentListItemDto {
  id: string;
  patientId: string;
  patientName: string;
  professionalId: string;
  specialtyId: string;
  start: string;
  durationMinutes: number;
  status: string;
  isSobreturno: boolean;
  isLateCancellation: boolean;
}
