export interface ProvincialLicenseDto {
  id?: string;
  number: string;
  province: string;
}

export interface ProfessionalDto {
  id: string;
  firstName: string;
  lastName: string;
  nationalLicenseMN: string;
  provincialLicenses: ProvincialLicenseDto[];
}

export interface CreateProfessionalRequest {
  firstName: string;
  lastName: string;
  nationalLicenseMN: string;
  provincialLicenses: Array<{ number: string; province: string }>;
}

export interface SpecialtyDto {
  id: string;
  code: string;
  name: string;
  defaultDurationMinutes: number;
  isActive: boolean;
}

export interface AssignSpecialtyRequest {
  isPrimary: boolean;
  durationOverrideMinutes?: number | null;
}
