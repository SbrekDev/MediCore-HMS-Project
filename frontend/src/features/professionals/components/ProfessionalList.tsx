import {
  Button,
  EmptyState,
  Table,
  Td,
  Th,
} from '../../../components/ui';
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
    return (
      <p className="px-5 py-8 text-center text-[13px] text-muted-2">
        Loading professionals…
      </p>
    );
  }

  if (professionals.length === 0) {
    return <EmptyState>No professionals found.</EmptyState>;
  }

  return (
    <Table>
      <thead>
        <tr>
          <Th>Name</Th>
          <Th>MN</Th>
          <Th>MPs</Th>
          <Th>Actions</Th>
        </tr>
      </thead>
      <tbody>
        {professionals.map((professional) => (
          <tr key={professional.id}>
            <Td>{`${professional.firstName} ${professional.lastName}`}</Td>
            <Td>{professional.nationalLicenseMN}</Td>
            <Td>{`${professional.provincialLicenses.length} MPs`}</Td>
            <Td>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => onAssign(professional)}
              >
                Assign specialty
              </Button>
            </Td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
}
