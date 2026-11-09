import { apiClient } from '../../api/axios';
import type {
  AppointmentListItemDto,
  AvailabilityResponse,
  CreateAppointmentRequest,
  RescheduleAppointmentRequest,
} from './types';

export async function getAvailability(
  professionalId: string,
  date: string
): Promise<AvailabilityResponse> {
  const response = await apiClient.get<AvailabilityResponse>(
    `/professionals/${professionalId}/availability`,
    { params: { date } }
  );
  return response.data;
}

export async function listAppointments(params: {
  from: string;
  to: string;
  professionalId?: string;
  patientId?: string;
  status?: string;
}): Promise<AppointmentListItemDto[]> {
  const response = await apiClient.get<AppointmentListItemDto[]>('/appointments', {
    params,
  });
  return response.data;
}

export async function createAppointment(
  request: CreateAppointmentRequest
): Promise<{ id: string }> {
  const response = await apiClient.post<{ id: string }>('/appointments', request);
  return response.data;
}

export async function rescheduleAppointment(
  id: string,
  request: RescheduleAppointmentRequest
): Promise<{ id: string; start: string }> {
  const response = await apiClient.post<{ id: string; start: string }>(
    `/appointments/${id}/reschedule`,
    request
  );
  return response.data;
}

export async function cancelAppointment(
  id: string
): Promise<{ id: string; status: string; isLateCancellation: boolean }> {
  const response = await apiClient.post<{ id: string; status: string; isLateCancellation: boolean }>(
    `/appointments/${id}/cancel`
  );
  return response.data;
}

export async function completeAppointment(id: string): Promise<{ id: string; status: string }> {
  const response = await apiClient.post<{ id: string; status: string }>(
    `/appointments/${id}/complete`
  );
  return response.data;
}

export async function noShowAppointment(id: string): Promise<{ id: string; status: string }> {
  const response = await apiClient.post<{ id: string; status: string }>(
    `/appointments/${id}/no-show`
  );
  return response.data;
}
