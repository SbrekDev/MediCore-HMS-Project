import type { ReactElement } from 'react';
import { Navigate } from 'react-router';
import { useSession } from '../auth/session';
import { RECEPTION_STAFF } from './rolePolicies';

/**
 * The index route has to land somewhere the reader can actually open: a professional
 * gets 403 on /patients, which used to make the shell's "Today" link a dead end.
 */
export function IndexRedirect(): ReactElement {
  const session = useSession();
  const canOpenPatients =
    session?.roles.some((role) => RECEPTION_STAFF.includes(role as 'Admin' | 'Receptionist')) ??
    false;

  return <Navigate to={canOpenPatients ? '/patients' : '/my-appointments'} replace />;
}
