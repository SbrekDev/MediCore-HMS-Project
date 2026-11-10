import { createBrowserRouter } from 'react-router';
import { Layout } from '../components/Layout';
import { RequireRole } from '../auth/RequireRole';
import { IndexRedirect } from './IndexRedirect';
import { ADMIN_ONLY, CLINICAL_STAFF, RECEPTION_STAFF } from './rolePolicies';
import { LoginPage } from '../features/auth/pages/LoginPage';
import { PatientsPage } from '../features/patients/pages/PatientsPage';
import { ProfessionalsPage } from '../features/professionals/pages/ProfessionalsPage';
import { AgendaPage } from '../features/agenda/pages/AgendaPage';
import { BookingPage } from '../features/booking/pages/BookingPage';
import { MyAppointmentsPage } from '../features/my-appointments/pages/MyAppointmentsPage';
import { TodayPage } from '../features/today/pages/TodayPage';

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
      { index: true, element: <IndexRedirect /> },
      {
        path: 'today',
        element: (
          <RequireRole allowed={CLINICAL_STAFF}>
            <TodayPage />
          </RequireRole>
        ),
      },
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
      {
        path: 'my-appointments',
        element: (
          <RequireRole allowed={CLINICAL_STAFF}>
            <MyAppointmentsPage />
          </RequireRole>
        ),
      },
    ],
  },
];

export const router = createBrowserRouter(routes);
