import type { AxiosError } from 'axios';

/**
 * Maps an API failure to a message the person editing the agenda can act on: the
 * two ways the agenda endpoints refuse a write that is otherwise well formed, and
 * a generic fallback for anything else (network, validation the UI cannot see).
 */
export function describeAgendaError(error: unknown, fallback: string): string {
  const status = (error as AxiosError | undefined)?.response?.status;

  if (status === 403) {
    return 'You do not have permission to edit this agenda.';
  }

  if (status === 409) {
    return 'An exception already exists for that date.';
  }

  return fallback;
}
