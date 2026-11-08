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

  const {
    data: professionals = [],
    isLoading: isLoadingProfessionals,
  } = useQuery({
    queryKey: ['professionals'],
    queryFn: getProfessionals,
  });

  const { data: specialties = [], isLoading: isLoadingSpecialties } = useQuery({
    queryKey: ['specialties'],
    queryFn: getSpecialties,
    enabled: selectedProfessional !== null,
  });

  const create = useMutation({
    mutationFn: createProfessional,
    onSuccess: () => {
      setShowForm(false);
      queryClient.invalidateQueries({ queryKey: ['professionals'] });
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
      queryClient.invalidateQueries({ queryKey: ['professionals'] });
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
        <ProfessionalForm
          onSubmit={(request) => create.mutate(request)}
          onCancel={() => setShowForm(false)}
        />
      )}

      {selectedProfessional && (
        <SpecialtyAssignment
          professional={selectedProfessional}
          specialties={specialties}
          isLoading={isLoadingSpecialties || assign.isPending}
          onAssign={handleAssign}
          onCancel={() => setSelectedProfessional(null)}
        />
      )}

      {!showForm && !selectedProfessional && (
        <ProfessionalList
          professionals={professionals}
          isLoading={isLoadingProfessionals}
          onAssign={setSelectedProfessional}
        />
      )}
    </div>
  );
}

