import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Alert,
  Button,
  Field,
  Input,
} from '../../../components/ui';
import type { CreateProfessionalRequest } from '../types';

const professionalSchema = z.object({
  firstName: z.string().min(1, 'Required'),
  lastName: z.string().min(1, 'Required'),
  nationalLicenseMN: z.string().min(1, 'Required'),
  provincialLicenses: z.array(
    z.object({
      number: z.string().min(1, 'Required'),
      province: z.string().min(1, 'Required'),
    })
  ),
});

type ProfessionalFormData = z.infer<typeof professionalSchema>;

interface ProfessionalFormProps {
  serverError?: string | null;
  onSubmit: (request: CreateProfessionalRequest) => void;
  onCancel: () => void;
}

export function ProfessionalForm({
  serverError,
  onSubmit,
  onCancel,
}: ProfessionalFormProps) {
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<ProfessionalFormData>({
    resolver: zodResolver(professionalSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      nationalLicenseMN: '',
      provincialLicenses: [],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'provincialLicenses',
  });

  function submitForm(values: ProfessionalFormData): void {
    onSubmit(values);
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

        <div className="md:col-span-2">
          <Field
            label="National license MN"
            htmlFor="nationalLicenseMN"
            error={errors.nationalLicenseMN?.message}
          >
            <Input id="nationalLicenseMN" {...register('nationalLicenseMN')} />
          </Field>
        </div>
      </div>

      <fieldset className="mt-5">
        <legend className="mb-3 text-sm font-semibold text-text">
          Provincial licenses
        </legend>
        {fields.map((field, index) => (
          <div
            key={field.id}
            className="mb-3 rounded-card border border-line bg-canvas p-4"
          >
            <div className="mb-3 flex items-center justify-between">
              <span className="text-xs font-semibold uppercase tracking-wide text-muted-2">
                License {index + 1}
              </span>
              <Button
                type="button"
                variant="danger"
                size="sm"
                onClick={() => remove(index)}
              >
                Remove
              </Button>
            </div>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <Field
                label={`License number ${index + 1}`}
                htmlFor={`license-number-${index}`}
                error={errors.provincialLicenses?.[index]?.number?.message}
              >
                <Input
                  id={`license-number-${index}`}
                  {...register(`provincialLicenses.${index}.number`)}
                />
              </Field>

              <Field
                label={`License province ${index + 1}`}
                htmlFor={`license-province-${index}`}
                error={errors.provincialLicenses?.[index]?.province?.message}
              >
                <Input
                  id={`license-province-${index}`}
                  {...register(`provincialLicenses.${index}.province`)}
                />
              </Field>
            </div>
          </div>
        ))}
        <Button
          type="button"
          variant="secondary"
          onClick={() => append({ number: '', province: '' })}
        >
          Add provincial license
        </Button>
      </fieldset>

      <div className="mt-5 flex items-center gap-2">
        <Button type="submit" variant="primary">
          Register professional
        </Button>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
