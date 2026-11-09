import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
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
  onSubmit: (request: CreateProfessionalRequest) => void;
  onCancel: () => void;
}

export function ProfessionalForm({ onSubmit, onCancel }: ProfessionalFormProps) {
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
    <form onSubmit={handleSubmit(submitForm)} noValidate>
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
        <label htmlFor="nationalLicenseMN">National license MN</label>
        <input id="nationalLicenseMN" {...register('nationalLicenseMN')} />
        {errors.nationalLicenseMN && <span>{errors.nationalLicenseMN.message}</span>}
      </div>

      <fieldset>
        <legend>Provincial licenses</legend>
        {fields.map((field, index) => (
          <div key={field.id}>
            <label htmlFor={`license-number-${index}`}>License number {index + 1}</label>
            <input
              id={`license-number-${index}`}
              {...register(`provincialLicenses.${index}.number`)}
            />
            {errors.provincialLicenses?.[index]?.number && (
              <span>{errors.provincialLicenses[index]?.number?.message}</span>
            )}

            <label htmlFor={`license-province-${index}`}>License province {index + 1}</label>
            <input
              id={`license-province-${index}`}
              {...register(`provincialLicenses.${index}.province`)}
            />
            {errors.provincialLicenses?.[index]?.province && (
              <span>{errors.provincialLicenses[index]?.province?.message}</span>
            )}

            <button type="button" onClick={() => remove(index)}>
              Remove
            </button>
          </div>
        ))}
        <button
          type="button"
          onClick={() => append({ number: '', province: '' })}
        >
          Add provincial license
        </button>
      </fieldset>

      <button type="submit">Register professional</button>
      <button type="button" onClick={onCancel}>
        Cancel
      </button>
    </form>
  );
}
