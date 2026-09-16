import { createBrowserRouter, Navigate } from 'react-router';
import { Layout } from '../components/Layout';
import { LoginPage } from '../features/auth/pages/LoginPage';
import { PatientsPage } from '../features/patients/pages/PatientsPage';
import { ProfessionalsPage } from '../features/professionals/pages/ProfessionalsPage';
import { AgendaPage } from '../features/agenda/pages/AgendaPage';
import { BookingPage } from '../features/booking/pages/BookingPage';

export const routes = [
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Navigate to="/patients" replace /> },
      { path: 'patients', element: <PatientsPage /> },
      { path: 'professionals', element: <ProfessionalsPage /> },
      { path: 'agenda', element: <AgendaPage /> },
      { path: 'booking', element: <BookingPage /> },
    ],
  },
];

export const router = createBrowserRouter(routes);
