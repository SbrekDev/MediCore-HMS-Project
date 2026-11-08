import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useSession } from './session';

interface RequireRoleProps {
  allowed: readonly string[];
  children: ReactNode;
}

/**
 * Shapes the UI by role. The server remains authoritative: this guard never
 * grants access, it only decides what the client renders.
 */
export function RequireRole({ allowed, children }: RequireRoleProps) {
  const session = useSession();
  const location = useLocation();

  if (!session) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  const authorized = session.roles.some((role) => allowed.includes(role));
  if (!authorized) {
    return (
      <p role="alert">You are not authorized to view this page.</p>
    );
  }

  return <>{children}</>;
}
