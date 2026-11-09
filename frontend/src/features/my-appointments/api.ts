import { listAppointments, completeAppointment, noShowAppointment } from '../booking/api';
import type { MyAppointmentListItemDto } from './types';

export async function listMyAppointments(params: {
  from: string;
  to: string;
}): Promise<MyAppointmentListItemDto[]> {
  return listAppointments(params) as Promise<MyAppointmentListItemDto[]>;
}

export { completeAppointment, noShowAppointment };
