import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { CreatePatientRequest } from '../types';
import {
  DOCUMENT_TYPES,
  patientSchema,
  type PatientFormData,
} from './patientFormSchema';

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
        <input
          id="responsiblePatientId"
          placeholder="Patient id (UUID), required for minors"
          {...register('responsiblePatientId')}
        />
        {errors.responsiblePatientId && <span>{errors.responsiblePatientId.message}</span>}
      </div>

      <button type="submit">Register patient</button>
      <button type="button" onClick={onCancel}>
        Cancel
      </button>
    </form>
  );
}
