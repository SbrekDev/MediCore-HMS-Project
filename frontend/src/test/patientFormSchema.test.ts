import { describe, it, expect } from 'vitest';
import {
  DOCUMENT_TYPES,
  MINOR_AGE,
  calculateAge,
  patientSchema,
} from '../features/patients/components/patientFormSchema';

function validValues(overrides: Record<string, unknown> = {}) {
  return {
    documentType: 'DNI',
    documentNumber: '30111222',
    firstName: 'Ana',
    lastName: 'Garcia',
    birthDate: '1990-05-15',
    contact: { mobilePhone: '1145678901', landlinePhone: '', email: '' },
    address: {
      street: 'Av. Santa Fe',
      number: '1234',
      city: 'CABA',
      province: 'Buenos Aires',
      postalCode: 'C1059',
    },
    responsiblePatientId: '',
    ...overrides,
  };
}

describe('patientFormSchema', () => {
  it('exposes one document-type list used by both the field and the schema', () => {
    expect(DOCUMENT_TYPES).toEqual(['DNI', 'Pasaporte', 'DNIExtranjero', 'LC/LE']);
    expect(DOCUMENT_TYPES.every((type) => patientSchema.safeParse(validValues({ documentType: type })).success)).toBe(true);
  });

  it('exports the minor age threshold instead of a magic number', () => {
    expect(MINOR_AGE).toBe(18);
  });

  it('calculates age from an ISO date and reports unparseable input as null', () => {
    expect(calculateAge('2000-01-01')).toBeGreaterThanOrEqual(MINOR_AGE);
    expect(calculateAge('not-a-date')).toBeNull();
    expect(calculateAge('')).toBeNull();
  });

  it('rejects an impossible calendar date instead of treating it as valid', () => {
    const result = patientSchema.safeParse(validValues({ birthDate: '2020-02-31' }));

    expect(result.success).toBe(false);
  });

  it('rejects a birth date in the future', () => {
    const nextYear = new Date().getFullYear() + 1;
    const result = patientSchema.safeParse(validValues({ birthDate: `${nextYear}-01-01` }));

    expect(result.success).toBe(false);
  });

  it('requires a responsible patient for a minor', () => {
    const minor = new Date();
    minor.setFullYear(minor.getFullYear() - (MINOR_AGE - 1));
    const birthDate = minor.toISOString().slice(0, 10);

    expect(patientSchema.safeParse(validValues({ birthDate })).success).toBe(false);
    expect(
      patientSchema.safeParse(
        validValues({ birthDate, responsiblePatientId: 'a1b2c3d4-0000-0000-0000-000000000000' })
      ).success
    ).toBe(true);
  });

  it('does not require a responsible patient for an adult', () => {
    expect(patientSchema.safeParse(validValues()).success).toBe(true);
  });

  it('rejects a responsible patient id that is not a UUID', () => {
    const result = patientSchema.safeParse(
      validValues({ responsiblePatientId: 'not-a-uuid' })
    );

    expect(result.success).toBe(false);
  });
});
