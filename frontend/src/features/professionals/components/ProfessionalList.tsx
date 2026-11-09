import type { ProfessionalDto } from '../types';

interface ProfessionalListProps {
  professionals: ProfessionalDto[];
  isLoading: boolean;
  onAssign: (professional: ProfessionalDto) => void;
}

export function ProfessionalList({
  professionals,
  isLoading,
  onAssign,
}: ProfessionalListProps) {
  if (isLoading) {
    return <p>Loading professionals…</p>;
  }

  if (professionals.length === 0) {
    return <p>No professionals found.</p>;
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Name</th>
          <th>MN</th>
          <th>MPs</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        {professionals.map((professional) => (
          <tr key={professional.id}>
            <td>{`${professional.firstName} ${professional.lastName}`}</td>
            <td>{professional.nationalLicenseMN}</td>
            <td>{`${professional.provincialLicenses.length} MPs`}</td>
            <td>
              <button
                type="button"
                onClick={() => onAssign(professional)}
              >
                Assign specialty
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
