import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Button,
  Card,
  CardHeader,
  PageHeader,
} from '../../../components/ui';
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
  const [selectedProfessional, setSelectedProfessional] =
    useState<ProfessionalDto | null>(null);
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

  function handleAssign(
    specialtyId: string,
    payload: AssignSpecialtyRequest
  ): void {
    if (!selectedProfessional) return;
    assign.mutate({
      professionalId: selectedProfessional.id,
      specialtyId,
      payload,
    });
  }

  return (
    <div data-testid="professionals-page" className="flex flex-col gap-5">
      <PageHeader
        title="Professionals"
        description="Manage medical staff and assign specialties."
        actions={
          !showForm &&
          !selectedProfessional && (
            <Button type="button" variant="primary" onClick={() => setShowForm(true)}>
              New professional
            </Button>
          )
        }
      />

      {showForm && (
        <Card>
          <CardHeader
            title="New professional"
            actions={
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  setShowForm(false);
                  setFormError(null);
                }}
              >
                Cancel
              </Button>
            }
          />
          <ProfessionalForm
            serverError={formError}
            onSubmit={(request) => create.mutate(request)}
            onCancel={() => {
              setShowForm(false);
              setFormError(null);
            }}
          />
        </Card>
      )}

      {selectedProfessional && (
        <Card>
          <CardHeader
            title={`Assign specialty to ${selectedProfessional.firstName} ${selectedProfessional.lastName}`}
            actions={
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  setSelectedProfessional(null);
                  setAssignmentError(null);
                }}
              >
                Cancel
              </Button>
            }
          />
          <div className="p-5">
            {assignmentError && (
              <div className="mb-5">
                <Alert>{assignmentError}</Alert>
              </div>
            )}
            {isSpecialtiesError && (
              <div className="mb-5">
                <Alert>Unable to load specialties. Please try again.</Alert>
              </div>
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
          </div>
        </Card>
      )}

      {!showForm && !selectedProfessional && (
        <Card>
          <CardHeader title="Professional directory" />
          {isProfessionalsError ? (
            <div className="p-5">
              <Alert>Unable to load professionals. Please try again.</Alert>
            </div>
          ) : (
            <ProfessionalList
              professionals={professionals}
              isLoading={isLoadingProfessionals}
              onAssign={setSelectedProfessional}
            />
          )}
        </Card>
      )}
    </div>
  );
}
