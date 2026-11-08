export type DocumentType = 'DNI' | 'Pasaporte' | 'DNIExtranjero' | 'LC/LE';

export interface PatientContactDto {
  mobilePhone: string;
  landlinePhone?: string | null;
  email?: string | null;
}

export interface PatientAddressDto {
  street: string;
  number: string;
  city: string;
  province: string;
  postalCode: string;
}

export interface PatientDto {
  id: string;
  documentType: DocumentType;
  documentNumber: string;
  firstName: string;
  lastName: string;
  birthDate: string;
  contact: PatientContactDto;
  address: PatientAddressDto;
  consentPending: boolean;
}

export interface CreatePatientRequest {
  documentType: DocumentType;
  documentNumber: string;
  firstName: string;
  lastName: string;
  birthDate: string;
  contact: {
    mobilePhone: string;
    landlinePhone?: string | null;
    email?: string | null;
  };
  address: {
    street: string;
    number: string;
    city: string;
    province: string;
    postalCode: string;
  };
  responsiblePatientId?: string | null;
  consentGiven?: boolean;
}
