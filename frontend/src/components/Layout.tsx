import { Link, Outlet } from 'react-router';

export function Layout() {
  return (
    <div>
      <header>
        <h1>MediCore HMS</h1>
        <nav>
          <Link to="/patients">Patients</Link>
          <Link to="/professionals">Professionals</Link>
          <Link to="/agenda">Agenda</Link>
          <Link to="/booking">Booking</Link>
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
