import { z } from 'zod';
import type { DocumentType } from '../types';

export const MINOR_AGE = 18;

export const DOCUMENT_TYPES: readonly DocumentType[] = [
  'DNI',
  'Pasaporte',
  'DNIExtranjero',
  'LC/LE',
];

const RESPONSIBLE_ID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Parses an `yyyy-mm-dd` input as a real calendar date in UTC. Returns null for
 * anything unparseable or impossible (for example 2020-02-31), so callers can
 * fail closed instead of silently accepting a malformed value.
 */
export function parseIsoDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value.trim());
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const parsed = new Date(Date.UTC(year, month - 1, day));

  const isSameCalendarDay =
    parsed.getUTCFullYear() === year &&
    parsed.getUTCMonth() === month - 1 &&
    parsed.getUTCDate() === day;

  return isSameCalendarDay ? parsed : null;
}

/** Whole years between the birth date and today, or null for unparseable input. */
export function calculateAge(birthDate: string): number | null {
  const parsed = parseIsoDate(birthDate);
  if (!parsed) {
    return null;
  }

  const now = new Date();
  let age = now.getFullYear() - parsed.getUTCFullYear();
  const hasNotHadBirthdayThisYear =
    now.getMonth() + 1 < parsed.getUTCMonth() + 1 ||
    (now.getMonth() + 1 === parsed.getUTCMonth() + 1 &&
      now.getDate() < parsed.getUTCDate());

  if (hasNotHadBirthdayThisYear) {
    age -= 1;
  }

  return age;
}

function todayUtc(): Date {
  const now = new Date();
  return new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()));
}

export const patientSchema = z
  .object({
    documentType: z.enum(DOCUMENT_TYPES as [DocumentType, ...DocumentType[]]),
    documentNumber: z.string().min(1, 'Required'),
    firstName: z.string().min(1, 'Required'),
    lastName: z.string().min(1, 'Required'),
    birthDate: z
      .string()
      .min(1, 'Required')
      .superRefine((value, context) => {
        const parsed = parseIsoDate(value);
        if (!parsed) {
          context.addIssue({ code: 'custom', message: 'Enter a valid date' });
          return;
        }
        if (parsed > todayUtc()) {
          context.addIssue({ code: 'custom', message: 'Birth date cannot be in the future' });
        }
      }),
    contact: z.object({
      mobilePhone: z.string().min(1, 'Required'),
      landlinePhone: z.string(),
      email: z.string(),
    }),
    address: z.object({
      street: z.string().min(1, 'Required'),
      number: z.string().min(1, 'Required'),
      city: z.string().min(1, 'Required'),
      province: z.string().min(1, 'Required'),
      postalCode: z.string().min(1, 'Required'),
    }),
    responsiblePatientId: z
      .string()
      .refine(
        (value) => value.trim().length === 0 || RESPONSIBLE_ID_PATTERN.test(value.trim()),
        'Enter a valid patient id (UUID)'
      ),
  })
  .refine(
    (values) => {
      const age = calculateAge(values.birthDate);
      // Fail closed: an unparseable birth date must not skip the minor rule.
      if (age === null) {
        return false;
      }
      if (age < MINOR_AGE) {
        return values.responsiblePatientId.trim().length > 0;
      }
      return true;
    },
    {
      message: 'A responsible patient is required for minors',
      path: ['responsiblePatientId'],
    }
  );

export type PatientFormData = z.infer<typeof patientSchema>;
