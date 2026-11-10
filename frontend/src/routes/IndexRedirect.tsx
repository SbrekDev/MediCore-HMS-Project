import type { ReactElement } from 'react';
import { Navigate } from 'react-router';

/**
 * The Today screen is the landing for every clinical role, so the index route
 * simply redirects there. The /today route itself is guarded by ClinicalStaff.
 */
export function IndexRedirect(): ReactElement {
  return <Navigate to="/today" replace />;
}
