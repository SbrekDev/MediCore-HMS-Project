import { useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router';
import { Alert, Button, Card, Field, Input } from '../../../components/ui';
import { login } from '../api';

interface LocationState {
  from?: string;
}

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      await login(email, password);
      const state = location.state as LocationState | null;
      navigate(state?.from ?? '/patients', { replace: true });
    } catch (caught) {
      const status = (caught as { response?: { status?: number } }).response?.status;
      setError(
        status === 401
          ? 'Invalid email or password.'
          : 'Unable to sign in. Please try again.'
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-svh items-center justify-center bg-canvas p-6">
      <Card className="w-full max-w-[420px]">
        <div className="flex flex-col gap-5 px-6 py-7">
          <div className="flex items-center gap-3">
            <div
              className="grid h-[34px] w-[34px] place-items-center rounded-[10px] bg-brand text-[15px] font-bold text-white"
              aria-hidden="true"
            >
              M
            </div>
            <div>
              <h1 className="text-[15px] font-semibold tracking-wide text-text">
                MediCore HMS
              </h1>
              <p className="text-[11px] uppercase tracking-widest text-muted-2">
                Sanatorio Los Ceibos
              </p>
            </div>
          </div>

          <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
            <Field label="Email" htmlFor="email">
              <Input
                id="email"
                name="email"
                type="email"
                autoComplete="username"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </Field>

            <Field label="Password" htmlFor="password">
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </Field>

            {error && <Alert>{error}</Alert>}

            <Button type="submit" variant="primary" className="w-full" disabled={submitting}>
              {submitting ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>
        </div>
      </Card>
    </div>
  );
}
