import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { routes } from './routes';

describe('App routing', () => {
  it('renders the MediCore HMS layout and the patients page', () => {
    const router = createMemoryRouter(routes, { initialEntries: ['/patients'] });
    render(<RouterProvider router={router} />);

    expect(screen.getByRole('heading', { name: /MediCore HMS/i })).toBeInTheDocument();
    expect(screen.getByTestId('patients-page')).toHaveTextContent('Patients');
  });

  it('redirects the root path to the patients page', () => {
    const router = createMemoryRouter(routes, { initialEntries: ['/'] });
    render(<RouterProvider router={router} />);

    expect(screen.getByTestId('patients-page')).toBeInTheDocument();
  });
});
