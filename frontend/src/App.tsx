import { RouterProvider } from 'react-router';
import { router } from './routes';
import { QueryProvider } from './providers/QueryProvider';

export function App() {
  return (
    <QueryProvider>
      <RouterProvider router={router} />
    </QueryProvider>
  );
}
