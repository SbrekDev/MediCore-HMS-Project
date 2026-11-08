import { apiClient } from '../../api/axios';
import type { CreatePatientRequest, PatientDto } from './types';

export async function searchPatients(search: string): Promise<PatientDto[]> {
  const response = await apiClient.get<PatientDto[]>('/patients', {
    params: { search },
  });
  return response.data;
}

export async function createPatient(
  request: CreatePatientRequest
): Promise<{ patientId: string }> {
  const response = await apiClient.post<{ patientId: string }>('/patients', request);
  return response.data;
}
