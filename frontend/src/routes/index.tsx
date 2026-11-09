import { createBrowserRouter, Navigate } from 'react-router';
import { Layout } from '../components/Layout';
import { RequireRole } from '../auth/RequireRole';
import { LoginPage } from '../features/auth/pages/LoginPage';
import { PatientsPage } from '../features/patients/pages/PatientsPage';
import { ProfessionalsPage } from '../features/professionals/pages/ProfessionalsPage';
import { AgendaPage } from '../features/agenda/pages/AgendaPage';
import { BookingPage } from '../features/booking/pages/BookingPage';

// Mirrors the server policy matrix: Admin+Receptionist for patients/booking,
// Admin for the professional catalog and the agenda (every agenda write is AdminOnly).
const RECEPTION_STAFF = ['Admin', 'Receptionist'] as const;
const CLINICAL_STAFF = ['Admin', 'Receptionist', 'Professional'] as const;
const ADMIN_ONLY = ['Admin'] as const;

export const routes = [
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: (
      <RequireRole allowed={CLINICAL_STAFF}>
        <Layout />
      </RequireRole>
    ),
    children: [
      { index: true, element: <Navigate to="/patients" replace /> },
      {
        path: 'patients',
        element: (
          <RequireRole allowed={RECEPTION_STAFF}>
            <PatientsPage />
          </RequireRole>
        ),
      },
      {
        path: 'professionals',
        element: (
          <RequireRole allowed={ADMIN_ONLY}>
            <ProfessionalsPage />
          </RequireRole>
        ),
      },
      {
        path: 'agenda',
        element: (
          <RequireRole allowed={ADMIN_ONLY}>
            <AgendaPage />
          </RequireRole>
        ),
      },
      {
        path: 'booking',
        element: (
          <RequireRole allowed={RECEPTION_STAFF}>
            <BookingPage />
          </RequireRole>
        ),
      },
    ],
  },
];

export const router = createBrowserRouter(routes);
