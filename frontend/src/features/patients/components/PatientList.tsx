import type { PatientDto } from '../types';

interface PatientListProps {
  patients: PatientDto[];
  isLoading: boolean;
}

export function PatientList({ patients, isLoading }: PatientListProps) {
  if (isLoading) {
    return <p>Loading patients…</p>;
  }

  if (patients.length === 0) {
    return <p>No patients found.</p>;
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Document</th>
          <th>Name</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {patients.map((patient) => (
          <tr key={patient.id}>
            <td>{`${patient.documentType} ${patient.documentNumber}`}</td>
            <td>{`${patient.firstName} ${patient.lastName}`}</td>
            <td>
              {patient.consentPending && (
                <span>Consent pending</span>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
