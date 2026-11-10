import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from 'react';

/**
 * The app's presentational vocabulary. Every screen composes these instead of writing
 * utility classes by hand, so the design tokens in `src/index.css` (`@theme`) are applied
 * consistently and a change of palette stays a token change.
 */

const BUTTON_VARIANTS = {
  primary: 'bg-brand text-white hover:bg-brand-ink border border-brand',
  secondary: 'bg-white text-text border border-line-strong hover:bg-canvas',
  ghost: 'bg-transparent text-muted border border-transparent hover:bg-canvas',
  danger: 'bg-white text-danger border border-danger/40 hover:bg-danger-wash',
} as const;

const BUTTON_SIZES = {
  sm: 'px-2.5 py-1.5 text-[12.5px]',
  md: 'px-3.5 py-2 text-[13px]',
} as const;

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: keyof typeof BUTTON_VARIANTS;
  size?: keyof typeof BUTTON_SIZES;
}

export function Button({
  variant = 'secondary',
  size = 'md',
  className = '',
  type = 'button',
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={`inline-flex items-center justify-center gap-2 rounded-control font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-55 ${BUTTON_VARIANTS[variant]} ${BUTTON_SIZES[size]} ${className}`}
      {...rest}
    />
  );
}

const FIELD_BASE =
  'w-full rounded-control border border-line-strong bg-white px-3 py-2 text-sm text-text placeholder:text-muted-2 focus:outline-none focus:ring-2 focus:ring-brand/45 disabled:bg-canvas';

export function Input({ className = '', ...rest }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={`${FIELD_BASE} ${className}`} {...rest} />;
}

export function Select({
  className = '',
  ...rest
}: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={`${FIELD_BASE} ${className}`} {...rest} />;
}

export function Textarea({
  className = '',
  ...rest
}: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={`${FIELD_BASE} ${className}`} {...rest} />;
}

interface FieldProps {
  label: string;
  htmlFor: string;
  error?: string;
  hint?: string;
  children: ReactNode;
}

/** Label above control, error below it, both tied to the control by id. */
export function Field({ label, htmlFor, error, hint, children }: FieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={htmlFor} className="text-[12.5px] font-medium text-text">
        {label}
      </label>
      {children}
      {hint && !error && <span className="text-[11.5px] text-muted-2">{hint}</span>}
      {error && (
        <span className="text-[11.5px] font-medium text-danger" role="alert">
          {error}
        </span>
      )}
    </div>
  );
}

interface CardProps {
  children: ReactNode;
  className?: string;
}

export function Card({ children, className = '' }: CardProps) {
  return (
    <section
      className={`overflow-hidden rounded-card border border-line bg-card shadow-card ${className}`}
    >
      {children}
    </section>
  );
}

export function CardHeader({
  title,
  actions,
}: {
  title: ReactNode;
  actions?: ReactNode;
}) {
  return (
    <div className="flex items-center gap-3 border-b border-line px-5 py-4">
      <h3 className="text-sm font-semibold text-text">{title}</h3>
      {actions && <div className="ml-auto flex items-center gap-2">{actions}</div>}
    </div>
  );
}

export function Table({ children }: { children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse text-[13.5px]">{children}</table>
    </div>
  );
}

export function Th({ children, className = '' }: { children?: ReactNode; className?: string }) {
  return (
    <th
      scope="col"
      className={`border-b border-line bg-canvas px-5 py-2.5 text-left text-[11px] font-semibold uppercase tracking-wide text-muted-2 ${className}`}
    >
      {children}
    </th>
  );
}

export function Td({ children, className = '' }: { children?: ReactNode; className?: string }) {
  return (
    <td className={`border-b border-line px-5 py-3 align-middle text-text ${className}`}>
      {children}
    </td>
  );
}

const TONES = {
  brand: 'bg-brand-wash text-brand-ink',
  ok: 'bg-ok-wash text-ok',
  warn: 'bg-warn-wash text-warn',
  danger: 'bg-danger-wash text-danger',
  neutral: 'bg-neutral-wash text-neutral',
} as const;

export function Badge({
  tone = 'neutral',
  children,
}: {
  tone?: keyof typeof TONES;
  children: ReactNode;
}) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-[11.5px] font-semibold ${TONES[tone]}`}
    >
      {children}
    </span>
  );
}

/** Page-level feedback: the one place a failure is announced to the reader. */
export function Alert({
  tone = 'danger',
  children,
}: {
  tone?: 'danger' | 'info';
  children: ReactNode;
}) {
  const styles =
    tone === 'danger'
      ? 'bg-danger-wash text-danger'
      : 'bg-brand-wash text-brand-ink';

  return (
    <p role="alert" className={`rounded-card px-4 py-3 text-sm ${styles}`}>
      {children}
    </p>
  );
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <p className="px-5 py-8 text-center text-[13px] text-muted-2">{children}</p>;
}

export function PageHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: ReactNode;
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-end gap-4">
      <div>
        <h2 className="text-lg font-medium tracking-tight text-text">{title}</h2>
        {description && <p className="mt-0.5 text-[13px] text-muted">{description}</p>}
      </div>
      {actions && <div className="ml-auto flex items-center gap-2">{actions}</div>}
    </div>
  );
}
