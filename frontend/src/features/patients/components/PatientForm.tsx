import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Button,
  Field,
  Input,
  Select,
} from '../../../components/ui';
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

export function PatientForm({
  serverError,
  onSubmit,
  onCancel,
}: PatientFormProps) {
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
      address: {
        street: '',
        number: '',
        city: '',
        province: '',
        postalCode: '',
      },
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
    <form onSubmit={handleSubmit(submitForm)} noValidate className="p-5">
      {serverError && (
        <div className="mb-5">
          <Alert>{serverError}</Alert>
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <Field
          label="Document type"
          htmlFor="documentType"
          error={errors.documentType?.message}
        >
          <Select id="documentType" {...register('documentType')}>
            {DOCUMENT_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label="Document number"
          htmlFor="documentNumber"
          error={errors.documentNumber?.message}
        >
          <Input id="documentNumber" {...register('documentNumber')} />
        </Field>

        <Field
          label="First name"
          htmlFor="firstName"
          error={errors.firstName?.message}
        >
          <Input id="firstName" {...register('firstName')} />
        </Field>

        <Field
          label="Last name"
          htmlFor="lastName"
          error={errors.lastName?.message}
        >
          <Input id="lastName" {...register('lastName')} />
        </Field>

        <Field
          label="Birth date"
          htmlFor="birthDate"
          error={errors.birthDate?.message}
        >
          <Input id="birthDate" type="date" {...register('birthDate')} />
        </Field>

        <Field
          label="Mobile phone"
          htmlFor="mobilePhone"
          error={errors.contact?.mobilePhone?.message}
        >
          <Input id="mobilePhone" {...register('contact.mobilePhone')} />
        </Field>

        <Field label="Landline phone" htmlFor="landlinePhone">
          <Input id="landlinePhone" {...register('contact.landlinePhone')} />
        </Field>

        <Field label="Email" htmlFor="email">
          <Input
            id="email"
            type="email"
            {...register('contact.email')}
          />
        </Field>
      </div>

      <fieldset className="mt-5">
        <legend className="mb-3 text-sm font-semibold text-text">Address</legend>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <Field
            label="Street name"
            htmlFor="street"
            error={errors.address?.street?.message}
          >
            <Input id="street" {...register('address.street')} />
          </Field>

          <Field
            label="Street number"
            htmlFor="number"
            error={errors.address?.number?.message}
          >
            <Input id="number" {...register('address.number')} />
          </Field>

          <Field
            label="City"
            htmlFor="city"
            error={errors.address?.city?.message}
          >
            <Input id="city" {...register('address.city')} />
          </Field>

          <Field
            label="Province"
            htmlFor="province"
            error={errors.address?.province?.message}
          >
            <Input id="province" {...register('address.province')} />
          </Field>

          <Field
            label="Postal code"
            htmlFor="postalCode"
            error={errors.address?.postalCode?.message}
          >
            <Input id="postalCode" {...register('address.postalCode')} />
          </Field>
        </div>
      </fieldset>

      <div className="mt-5 md:max-w-[50%]">
        <Field
          label="Responsible patient ID"
          htmlFor="responsiblePatientId"
          error={errors.responsiblePatientId?.message}
          hint="Patient id (UUID), required for minors"
        >
          <Input
            id="responsiblePatientId"
            placeholder="Patient id (UUID), required for minors"
            {...register('responsiblePatientId')}
          />
        </Field>
      </div>

      <div className="mt-5 flex items-center gap-2">
        <Button type="submit" variant="primary">
          Register patient
        </Button>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
