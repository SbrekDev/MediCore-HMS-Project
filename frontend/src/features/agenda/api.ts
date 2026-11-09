import { apiClient } from '../../api/axios';
import type {
  AgendaDto,
  CreateAgendaExceptionRequest,
  UpdateAgendaRequest,
} from './types';

export async function getAgenda(professionalId: string): Promise<AgendaDto> {
  const response = await apiClient.get<AgendaDto>(`/professionals/${professionalId}/agenda`);
  return response.data;
}

export async function updateAgenda(
  professionalId: string,
  request: UpdateAgendaRequest
): Promise<void> {
  await apiClient.put(`/professionals/${professionalId}/agenda`, request);
}

export async function createAgendaException(
  professionalId: string,
  request: CreateAgendaExceptionRequest
): Promise<string> {
  const response = await apiClient.post<string>(
    `/professionals/${professionalId}/agenda/exceptions`,
    request
  );
  return response.data;
}

export async function deleteAgendaException(
  professionalId: string,
  exceptionId: string
): Promise<void> {
  await apiClient.delete(`/professionals/${professionalId}/agenda/exceptions/${exceptionId}`);
}
