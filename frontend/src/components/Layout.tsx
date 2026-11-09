import { Link, Outlet, useNavigate } from 'react-router';
import { decodeProfessionalId, useSession } from '../auth/session';
import { logout } from '../features/auth/api';

export function Layout() {
  const session = useSession();
  const navigate = useNavigate();
  const canOpenAgenda = session?.roles.includes('Admin') ?? false;
  const professionalId = session ? decodeProfessionalId(session.accessToken) : null;

  async function handleSignOut() {
    await logout();
    navigate('/login', { replace: true });
  }

  return (
    <div>
      <header>
        <h1>MediCore HMS</h1>
        <nav>
          <Link to="/patients">Patients</Link>
          <Link to="/professionals">Professionals</Link>
          {canOpenAgenda && <Link to="/agenda">Agenda</Link>}
          {professionalId && <Link to="/my-appointments">My appointments</Link>}
          <Link to="/booking">Booking</Link>
        </nav>
        {session && (
          <div>
            <span data-testid="current-user">{session.email}</span>
            <button type="button" onClick={handleSignOut}>
              Sign out
            </button>
          </div>
        )}
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
