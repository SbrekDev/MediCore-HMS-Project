import { Badge, EmptyState, Table, Td, Th } from '../../../components/ui';
import type { PatientDto } from '../types';

interface PatientListProps {
  patients: PatientDto[];
  isLoading: boolean;
}

export function PatientList({ patients, isLoading }: PatientListProps) {
  if (isLoading) {
    return (
      <p className="px-5 py-8 text-center text-[13px] text-muted-2">
        Loading patients…
      </p>
    );
  }

  if (patients.length === 0) {
    return <EmptyState>No patients found.</EmptyState>;
  }

  return (
    <Table>
      <thead>
        <tr>
          <Th>Document</Th>
          <Th>Name</Th>
          <Th>Status</Th>
        </tr>
      </thead>
      <tbody>
        {patients.map((patient) => (
          <tr key={patient.id}>
            <Td>{`${patient.documentType} ${patient.documentNumber}`}</Td>
            <Td>{`${patient.firstName} ${patient.lastName}`}</Td>
            <Td>
              {patient.consentPending && (
                <Badge tone="warn">Consent pending</Badge>
              )}
            </Td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
}
