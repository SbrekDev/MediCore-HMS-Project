import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { CreatePatientRequest, DocumentType } from '../types';

const DOCUMENT_TYPES: DocumentType[] = ['DNI', 'Pasaporte', 'DNIExtranjero', 'LC/LE'];

const patientSchema = z
  .object({
    documentType: z.enum(['DNI', 'Pasaporte', 'DNIExtranjero', 'LC/LE']),
    documentNumber: z.string().min(1, 'Required'),
    firstName: z.string().min(1, 'Required'),
    lastName: z.string().min(1, 'Required'),
    birthDate: z
      .string()
      .min(1, 'Required')
      .refine((value) => {
        const [year, month, day] = value.split('-').map(Number);
        if (!year || !month || !day) return false;
        const input = new Date(Date.UTC(year, month - 1, day));
        const now = new Date();
        const today = new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()));
        return input <= today;
      }, 'Birth date cannot be in the future'),
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
    responsiblePatientId: z.string(),
  })
  .refine(
    (values) => {
      const age = calculateAge(values.birthDate);
      if (age === null) return true;
      if (age < 18) {
        return values.responsiblePatientId.length > 0;
      }
      return true;
    },
    {
      message: 'A responsible patient is required for minors',
      path: ['responsiblePatientId'],
    }
  );

type PatientFormData = z.infer<typeof patientSchema>;

function calculateAge(birthDate: string): number | null {
  const [year, month, day] = birthDate.split('-').map(Number);
  if (!year || !month || !day) return null;

  const now = new Date();
  let age = now.getFullYear() - year;
  const currentMonth = now.getMonth() + 1;
  const currentDay = now.getDate();
  if (currentMonth < month || (currentMonth === month && currentDay < day)) {
    age--;
  }
  return age;
}

function emptyToNull(value: string): string | null {
  return value.trim().length > 0 ? value : null;
}

interface PatientFormProps {
  serverError: string | null;
  onSubmit: (request: CreatePatientRequest) => void;
  onCancel: () => void;
}

export function PatientForm({ serverError, onSubmit, onCancel }: PatientFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<PatientFormData>({
    resolver: zodResolver(patientSchema),
    defaultValues: {
      documentType: 'DNI',
      documentNumber: '',
      firstName: '',
      lastName: '',
      birthDate: '',
      contact: { mobilePhone: '', landlinePhone: '', email: '' },
      address: { street: '', number: '', city: '', province: '', postalCode: '' },
      responsiblePatientId: '',
    },
  });

  function submitForm(values: PatientFormData): void {
    const request: CreatePatientRequest = {
      documentType: values.documentType,
      documentNumber: values.documentNumber,
      firstName: values.firstName,
      lastName: values.lastName,
      birthDate: values.birthDate,
      contact: {
        mobilePhone: values.contact.mobilePhone,
        landlinePhone: emptyToNull(values.contact.landlinePhone),
        email: emptyToNull(values.contact.email),
      },
      address: values.address,
      responsiblePatientId: emptyToNull(values.responsiblePatientId),
    };
    onSubmit(request);
  }

  return (
    <form onSubmit={handleSubmit(submitForm)} noValidate>
      {serverError && <p role="alert">{serverError}</p>}

      <div>
        <label htmlFor="documentType">Document type</label>
        <select id="documentType" {...register('documentType')}>
          {DOCUMENT_TYPES.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
        {errors.documentType && <span>{errors.documentType.message}</span>}
      </div>

      <div>
        <label htmlFor="documentNumber">Document number</label>
        <input id="documentNumber" {...register('documentNumber')} />
        {errors.documentNumber && <span>{errors.documentNumber.message}</span>}
      </div>

      <div>
        <label htmlFor="firstName">First name</label>
        <input id="firstName" {...register('firstName')} />
        {errors.firstName && <span>{errors.firstName.message}</span>}
      </div>

      <div>
        <label htmlFor="lastName">Last name</label>
        <input id="lastName" {...register('lastName')} />
        {errors.lastName && <span>{errors.lastName.message}</span>}
      </div>

      <div>
        <label htmlFor="birthDate">Birth date</label>
        <input id="birthDate" type="date" {...register('birthDate')} />
        {errors.birthDate && <span>{errors.birthDate.message}</span>}
      </div>

      <div>
        <label htmlFor="mobilePhone">Mobile phone</label>
        <input id="mobilePhone" {...register('contact.mobilePhone')} />
        {errors.contact?.mobilePhone && <span>{errors.contact.mobilePhone.message}</span>}
      </div>

      <div>
        <label htmlFor="landlinePhone">Landline phone</label>
        <input id="landlinePhone" {...register('contact.landlinePhone')} />
      </div>

      <div>
        <label htmlFor="email">Email</label>
        <input id="email" type="email" {...register('contact.email')} />
      </div>

      <fieldset>
        <legend>Address</legend>
        <div>
          <label htmlFor="street">Street name</label>
          <input id="street" {...register('address.street')} />
          {errors.address?.street && <span>{errors.address.street.message}</span>}
        </div>
        <div>
          <label htmlFor="number">Street number</label>
          <input id="number" {...register('address.number')} />
          {errors.address?.number && <span>{errors.address.number.message}</span>}
        </div>
        <div>
          <label htmlFor="city">City</label>
          <input id="city" {...register('address.city')} />
          {errors.address?.city && <span>{errors.address.city.message}</span>}
        </div>
        <div>
          <label htmlFor="province">Province</label>
          <input id="province" {...register('address.province')} />
          {errors.address?.province && <span>{errors.address.province.message}</span>}
        </div>
        <div>
          <label htmlFor="postalCode">Postal code</label>
          <input id="postalCode" {...register('address.postalCode')} />
          {errors.address?.postalCode && <span>{errors.address.postalCode.message}</span>}
        </div>
      </fieldset>

      <div>
        <label htmlFor="responsiblePatientId">Responsible patient ID</label>
        <input id="responsiblePatientId" {...register('responsiblePatientId')} />
        {errors.responsiblePatientId && <span>{errors.responsiblePatientId.message}</span>}
      </div>

      <button type="submit">Register patient</button>
      <button type="button" onClick={onCancel}>
        Cancel
      </button>
    </form>
  );
}
