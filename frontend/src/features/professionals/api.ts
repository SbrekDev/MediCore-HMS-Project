import { apiClient } from '../../api/axios';
import type {
  AssignSpecialtyRequest,
  CreateProfessionalRequest,
  ProfessionalDto,
  SpecialtyDto,
} from './types';

export async function getProfessionals(): Promise<ProfessionalDto[]> {
  const response = await apiClient.get<ProfessionalDto[]>('/professionals');
  return response.data;
}

export async function createProfessional(
  request: CreateProfessionalRequest
): Promise<{ professionalId: string }> {
  const response = await apiClient.post<{ professionalId: string }>(
    '/professionals',
    request
  );
  return response.data;
}

export async function getSpecialties(): Promise<SpecialtyDto[]> {
  const response = await apiClient.get<SpecialtyDto[]>('/specialties', {
    params: { activeOnly: true },
  });
  return response.data;
}

export async function assignSpecialty(
  professionalId: string,
  specialtyId: string,
  request: AssignSpecialtyRequest
): Promise<void> {
  await apiClient.put(
    `/professionals/${professionalId}/specialties/${specialtyId}`,
    request
  );
}
