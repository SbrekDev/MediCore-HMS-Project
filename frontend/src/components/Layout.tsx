import { type ReactElement, type ReactNode } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router';
import { decodeProfessionalId, useSession } from '../auth/session';
import { logout } from '../features/auth/api';

const RECEPTION_STAFF = ['Admin', 'Receptionist'];

const PAGE_TITLES: Record<string, string> = {
  '/': 'Today',
  '/patients': 'Patients',
  '/professionals': 'Professionals',
  '/agenda': 'Agenda',
  '/booking': 'Booking',
  '/my-appointments': 'My appointments',
};

function usePageTitle(): string {
  const { pathname } = useLocation();
  return PAGE_TITLES[pathname] ?? 'MediCore HMS';
}

function hasAnyRole(roles: string[], allowed: string[]): boolean {
  return roles.some((role) => allowed.includes(role));
}

function initials(value: string): string {
  return value.slice(0, 2).toUpperCase();
}

export function Layout(): ReactElement {
  const session = useSession();
  const navigate = useNavigate();
  const pageTitle = usePageTitle();

  if (!session) {
    return <Outlet />;
  }

  const professionalId = decodeProfessionalId(session.accessToken);
  const canViewPatients = hasAnyRole(session.roles, RECEPTION_STAFF);
  const canViewBooking = hasAnyRole(session.roles, RECEPTION_STAFF);
  const canViewAgenda = session.roles.includes('Admin');
  const canViewProfessionals = session.roles.includes('Admin');

  async function handleSignOut(): Promise<void> {
    await logout();
    navigate('/login', { replace: true });
  }

  return (
    <div className="grid min-h-svh grid-cols-[72px_1fr] bg-canvas text-text lg:grid-cols-[248px_1fr]">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-50 focus:rounded-control focus:bg-card focus:px-3 focus:py-2 focus:text-text"
      >
        Skip to content
      </a>

      <aside className="flex flex-col gap-6 bg-nav px-4 py-5 text-[#c3d2cf]">
        <div className="flex items-center gap-2.5 px-2 pt-1">
          <div
            className="grid h-[34px] w-[34px] place-items-center rounded-[10px] bg-gradient-to-br from-brand to-brand-ink text-[15px] font-bold text-white"
            aria-hidden="true"
          >
            M
          </div>
          <div className="sr-only lg:not-sr-only lg:static">
            <p className="text-[15px] font-semibold tracking-wide text-[#f8fafc]">
              MediCore HMS
            </p>
            <p className="text-[11px] uppercase tracking-widest text-[#7f9a96]">
              Sanatorio Los Ceibos
            </p>
          </div>
        </div>

        <nav className="flex flex-col gap-0.5" aria-label="Main">
          <NavItem to="/" icon={<TodayIcon />} end>
            Today
          </NavItem>

          {(canViewPatients || canViewBooking) && <NavLabel>Front desk</NavLabel>}
          {canViewPatients && (
            <NavItem to="/patients" icon={<PatientsIcon />}>
              Patients
            </NavItem>
          )}
          {canViewBooking && (
            <NavItem to="/booking" icon={<BookingIcon />}>
              Booking
            </NavItem>
          )}

          {(canViewAgenda || professionalId) && <NavLabel>Clinical</NavLabel>}
          {canViewAgenda && (
            <NavItem to="/agenda" icon={<AgendaIcon />}>
              Agenda
            </NavItem>
          )}
          {professionalId && (
            <NavItem to="/my-appointments" icon={<ClockIcon />}>
              My appointments
            </NavItem>
          )}

          {canViewProfessionals && <NavLabel>Administration</NavLabel>}
          {canViewProfessionals && (
            <NavItem to="/professionals" icon={<ProfessionalsIcon />}>
              Professionals
            </NavItem>
          )}
        </nav>

        <div className="mt-auto flex items-center gap-2.5 border-t border-[#16302f] pt-3.5">
          <div
            className="grid h-8 w-8 place-items-center rounded-full bg-blue-800 text-xs font-bold text-blue-100"
            aria-hidden="true"
          >
            {initials(session.email)}
          </div>
          <div className="sr-only lg:not-sr-only lg:static">
            <p className="text-[13px] text-[#e6efed]">{session.email}</p>
            <p className="text-[11px] text-[#7f9a96]">
              {session.roles.join(', ')}
            </p>
          </div>
        </div>
      </aside>

      <div className="flex min-w-0 flex-col">
        <header className="flex h-16 items-center gap-4 border-b border-line bg-card px-7">
          <div className="min-w-0">
            <p className="text-xs text-muted-2">MediCore HMS</p>
            <h1 className="truncate text-[17px] font-medium tracking-tight text-text">
              {pageTitle}
            </h1>
          </div>

          <div className="ml-auto flex items-center gap-4">
            <div className="hidden text-right sm:block">
              <p
                className="text-[13px] font-medium text-text"
                data-testid="current-user"
              >
                {session.email}
              </p>
              <p className="text-xs text-muted">
                {session.roles.join(', ')}
              </p>
            </div>
            <button
              type="button"
              onClick={handleSignOut}
              className="rounded-control border border-line-strong bg-white px-3.5 py-2 text-[13px] font-semibold text-text hover:bg-canvas"
            >
              Sign out
            </button>
          </div>
        </header>

        <main id="main" className="flex-1 p-7">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

interface NavItemProps {
  to: string;
  icon: ReactElement;
  children: ReactNode;
  end?: boolean;
}

function NavItem({ to, icon, children, end }: NavItemProps): ReactElement {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        `flex items-center justify-center gap-2.5 rounded-control px-2.5 py-2 text-[13.5px] transition-colors lg:justify-start ${
          isActive
            ? 'bg-nav-active font-semibold text-white'
            : 'text-[#b8cbc7] hover:bg-nav-hover hover:text-white'
        }`
      }
    >
      {({ isActive }) => (
        <>
          <span
            className={isActive ? 'text-white' : 'text-[#7f9a96]'}
            aria-hidden="true"
          >
            {icon}
          </span>
          <span className="sr-only lg:not-sr-only lg:static">{children}</span>
        </>
      )}
    </NavLink>
  );
}

function NavLabel({ children }: { children: ReactNode }): ReactElement {
  return (
    <p className="sr-only px-2.5 pb-1.5 pt-3.5 text-[10.5px] uppercase tracking-widest text-muted lg:not-sr-only lg:static">
      {children}
    </p>
  );
}

function TodayIcon(): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <rect x="3" y="5" width="18" height="16" rx="3" />
      <path d="M8 3v4M16 3v4M3 11h18" />
    </svg>
  );
}

function PatientsIcon(): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle cx="12" cy="8" r="3.5" />
      <path d="M5 20c0-3.5 3.1-6 7-6s7 2.5 7 6" />
    </svg>
  );
}

function BookingIcon(): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M4 6h16M4 12h16M4 18h10" />
    </svg>
  );
}

function AgendaIcon(): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M6 4v6a6 6 0 0 0 12 0V4" />
      <path d="M12 16v4" />
    </svg>
  );
}

function ClockIcon(): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="8" />
      <path d="M12 8v4l3 2" />
    </svg>
  );
}

function ProfessionalsIcon(): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <rect x="4" y="4" width="16" height="16" rx="3" />
      <path d="M9 9h6M9 13h6M9 17h3" />
    </svg>
  );
}
