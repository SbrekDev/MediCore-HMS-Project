export interface AppointmentCountsDto {
  total: number;
  confirmed: number;
  completed: number;
  noShow: number;
  cancelled: number;
  sobreturnos: number;
}

export interface PatientCountsDto {
  total: number;
  addedInRange: number;
}

export interface ProfessionalCountsDto {
  total: number;
  withTemplate: number;
}

export interface DashboardSummaryDto {
  from: string;
  to: string;
  appointments: AppointmentCountsDto;
  patients: PatientCountsDto;
  professionals: ProfessionalCountsDto;
}

export type { AppointmentListItemDto } from '../booking/types';
