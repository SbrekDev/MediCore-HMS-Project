import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  assignSpecialty,
  createProfessional,
  getProfessionals,
  getSpecialties,
} from '../api';
import { ProfessionalForm } from '../components/ProfessionalForm';
import { ProfessionalList } from '../components/ProfessionalList';
import { SpecialtyAssignment } from '../components/SpecialtyAssignment';
import type { AssignSpecialtyRequest, ProfessionalDto } from '../types';

export function ProfessionalsPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [selectedProfessional, setSelectedProfessional] = useState<ProfessionalDto | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [assignmentError, setAssignmentError] = useState<string | null>(null);

  const {
    data: professionals = [],
    isLoading: isLoadingProfessionals,
    isError: isProfessionalsError,
  } = useQuery({
    queryKey: ['professionals'],
    queryFn: getProfessionals,
  });

  const {
    data: specialties = [],
    isLoading: isLoadingSpecialties,
    isError: isSpecialtiesError,
  } = useQuery({
    queryKey: ['specialties'],
    queryFn: getSpecialties,
    enabled: selectedProfessional !== null,
  });

  const create = useMutation({
    mutationFn: createProfessional,
    onSuccess: () => {
      setShowForm(false);
      setFormError(null);
      queryClient.invalidateQueries({ queryKey: ['professionals'] });
    },
    onError: () => {
      setFormError('Unable to register the professional. Please try again.');
    },
  });

  const assign = useMutation({
    mutationFn: (request: {
      professionalId: string;
      specialtyId: string;
      payload: AssignSpecialtyRequest;
    }) => assignSpecialty(request.professionalId, request.specialtyId, request.payload),
    onSuccess: () => {
      setSelectedProfessional(null);
      setAssignmentError(null);
      queryClient.invalidateQueries({ queryKey: ['professionals'] });
    },
    onError: () => {
      setAssignmentError('Unable to assign the specialty. Please try again.');
    },
  });

  function handleAssign(specialtyId: string, payload: AssignSpecialtyRequest): void {
    if (!selectedProfessional) return;
    assign.mutate({
      professionalId: selectedProfessional.id,
      specialtyId,
      payload,
    });
  }

  return (
    <div data-testid="professionals-page">
      <h2>Professionals</h2>

      {!showForm && !selectedProfessional && (
        <button type="button" onClick={() => setShowForm(true)}>
          New professional
        </button>
      )}

      {showForm && (
        <>
          {formError && <p role="alert">{formError}</p>}
          <ProfessionalForm
            onSubmit={(request) => create.mutate(request)}
            onCancel={() => {
              setShowForm(false);
              setFormError(null);
            }}
          />
        </>
      )}

      {selectedProfessional && (
        <>
          {assignmentError && <p role="alert">{assignmentError}</p>}
          {isSpecialtiesError && (
            <p role="alert">Unable to load specialties. Please try again.</p>
          )}
          <SpecialtyAssignment
            professional={selectedProfessional}
            specialties={specialties}
            isLoading={isLoadingSpecialties || assign.isPending}
            onAssign={handleAssign}
            onCancel={() => {
              setSelectedProfessional(null);
              setAssignmentError(null);
            }}
          />
        </>
      )}

      {!showForm && !selectedProfessional && (
        isProfessionalsError ? (
          <p role="alert">Unable to load professionals. Please try again.</p>
        ) : (
          <ProfessionalList
            professionals={professionals}
            isLoading={isLoadingProfessionals}
            onAssign={setSelectedProfessional}
          />
        )
      )}
    </div>
  );
}

