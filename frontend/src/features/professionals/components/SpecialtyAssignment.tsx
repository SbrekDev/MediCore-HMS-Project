import { useState, type FormEvent } from 'react';
import type { AssignSpecialtyRequest, ProfessionalDto, SpecialtyDto } from '../types';

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
    <form onSubmit={handleSubmit}>
      <h3>
        Assign specialty to {professional.firstName} {professional.lastName}
      </h3>

      <div>
        <label htmlFor="specialty">Specialty</label>
        <select
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
        </select>
      </div>

      <div>
        <label htmlFor="isPrimary">
          <input
            id="isPrimary"
            type="checkbox"
            checked={isPrimary}
            onChange={(event) => setIsPrimary(event.target.checked)}
          />
          Primary
        </label>
      </div>

      <div>
        <label htmlFor="durationOverride">Duration override (minutes)</label>
        <input
          id="durationOverride"
          type="number"
          value={durationOverride}
          onChange={(event) => setDurationOverride(event.target.value)}
          placeholder="Use specialty default"
        />
      </div>

      <button type="submit" disabled={isLoading || specialtyId === ''}>
        Save assignment
      </button>
      <button type="button" onClick={onCancel}>
        Cancel
      </button>
    </form>
  );
}
