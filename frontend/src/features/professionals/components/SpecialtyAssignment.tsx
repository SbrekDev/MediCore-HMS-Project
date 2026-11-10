import type { ChangeEvent } from 'react';
import { useState, type FormEvent } from 'react';
import {
  Button,
  Field,
  Input,
  Select,
  Checkbox,
} from '../../../components/ui';
import type {
  AssignSpecialtyRequest,
  ProfessionalDto,
  SpecialtyDto,
} from '../types';

interface SpecialtyAssignmentProps {
  professional: ProfessionalDto;
  specialties: SpecialtyDto[];
  isLoading: boolean;
  onAssign: (specialtyId: string, request: AssignSpecialtyRequest) => void;
  onCancel: () => void;
}

export function SpecialtyAssignment({
  professional,
  specialties,
  isLoading,
  onAssign,
  onCancel,
}: SpecialtyAssignmentProps) {
  const [specialtyId, setSpecialtyId] = useState('');
  const [isPrimary, setIsPrimary] = useState(false);
  const [durationOverride, setDurationOverride] = useState('');

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    if (specialtyId === '') return;
    const parsed = durationOverride === '' ? null : Number(durationOverride);
    onAssign(specialtyId, {
      isPrimary,
      durationOverrideMinutes: parsed,
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <Field label="Specialty" htmlFor="specialty">
        <Select
          id="specialty"
          value={specialtyId}
          onChange={(event) => setSpecialtyId(event.target.value)}
          required
        >
          <option value="">Select a specialty</option>
          {specialties.map((specialty) => (
            <option key={specialty.id} value={specialty.id}>
              {specialty.name}
            </option>
          ))}
        </Select>
      </Field>

      <div className="rounded-card border border-line bg-canvas p-4">
        <Checkbox
          id="isPrimary"
          checked={isPrimary}
          onChange={(event: ChangeEvent<HTMLInputElement>) => setIsPrimary(event.target.checked)}
          label={`Mark this as ${professional.firstName} ${professional.lastName}'s primary`}
        />
      </div>

      <Field
        label="Duration override (minutes)"
        htmlFor="durationOverride"
      >
        <Input
          id="durationOverride"
          type="number"
          value={durationOverride}
          onChange={(event) => setDurationOverride(event.target.value)}
          placeholder="Use specialty default"
        />
      </Field>

      <div className="flex items-center gap-2">
        <Button
          type="submit"
          variant="primary"
          disabled={isLoading || specialtyId === ''}
        >
          Save assignment
        </Button>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
