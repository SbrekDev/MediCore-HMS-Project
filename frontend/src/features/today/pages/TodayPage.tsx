import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { toDayRangeStrings, pad } from '../../booking/components/bookingSchema';
import { listAppointments } from '../../booking/api';
import { getDashboardSummary } from '../api';
import type { AppointmentListItemDto, DashboardSummaryDto } from '../types';

function todayLocalString(): string {
  const now = new Date();
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

function formatTime(start: string): string {
  const date = new Date(start);
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function statusClass(status: string): string {
  switch (status) {
    case 'Confirmed':
      return 'bg-brand-wash text-brand-ink';
    case 'Completed':
      return 'bg-ok-wash text-ok';
    case 'NoShow':
      return 'bg-danger-wash text-danger';
    case 'Cancelled':
      return 'bg-neutral-wash text-neutral';
    default:
      return 'bg-neutral-wash text-neutral';
  }
}

function statusLabel(status: string): string {
  return status === 'NoShow' ? 'No-show' : status;
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
  return `${first}${last}`.toUpperCase();
}

export function TodayPage() {
  const [date, setDate] = useState<string>(todayLocalString());

  const range = useMemo(() => toDayRangeStrings(date), [date]);

  const {
    data: summary,
    error: summaryError,
  } = useQuery<DashboardSummaryDto>({
    queryKey: ['dashboard-summary', date],
    queryFn: () => getDashboardSummary({ from: range.from, to: range.to }),
    enabled: date !== '',
  });

  const {
    data: appointments = [],
    isLoading: appointmentsLoading,
    error: appointmentsError,
  } = useQuery<AppointmentListItemDto[]>({
    queryKey: ['today-appointments', date],
    queryFn: () => listAppointments({ from: range.from, to: range.to }),
    enabled: date !== '',
  });

  // A professional account without a professional link is refused by the appointments
  // list with Appointment.NotOwner. That is its real state, not a failure to retry.
  const isNotLinked = useMemo(() => {
    const response = (
      appointmentsError as { response?: { status?: number; data?: { code?: string } } } | null
    )?.response;
    return response?.status === 403 && response.data?.code === 'Appointment.NotOwner';
  }, [appointmentsError]);

  const errorMessage = useMemo(() => {
    if (isNotLinked) {
      return null;
    }
    if (summaryError) {
      return 'Unable to load the dashboard summary. Please try again.';
    }
    if (appointmentsError) {
      return 'Unable to load the appointments. Please try again.';
    }
    return null;
  }, [isNotLinked, summaryError, appointmentsError]);

  return (
    <div data-testid="today-page" className="flex flex-col gap-5">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-medium tracking-tight text-text">
          Today&apos;s overview
        </h2>
        <input
          id="today-date"
          type="date"
          value={date}
          onChange={(event) => setDate(event.target.value)}
          className="rounded-control border border-line bg-white px-3 py-2 text-sm text-text focus:outline-none focus:ring-2 focus:ring-brand/45"
        />
      </div>

      {isNotLinked && (
        <p className="rounded-card border border-line bg-card p-4 text-sm text-muted">
          This account is not linked to a professional, so there are no personal
          appointments to show. The totals above cover the whole institution.
        </p>
      )}

      {errorMessage && (
        <p role="alert" className="rounded-card bg-danger-wash p-4 text-sm text-danger">
          {errorMessage}
        </p>
      )}

      <section
        className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4"
        aria-label="Overview"
      >
        <KpiTile
          label="Appointments today"
          value={summary?.appointments.total}
          subline={
            summary &&
            `All professionals · ${summary.appointments.completed} completed · ${summary.appointments.noShow} no-show`
          }
        />
        <KpiTile
          label="Patients registered"
          value={summary?.patients.total}
          subline={summary && `${summary.patients.addedInRange} added in range`}
        />
        <KpiTile
          label="Professionals"
          value={summary?.professionals.total}
          subline={summary && `${summary.professionals.withTemplate} with a template`}
        />
        <KpiTile
          label="Sobreturnos today"
          value={summary?.appointments.sobreturnos}
          subline={summary ? 'Used today' : undefined}
        />
      </section>

      <section className="rounded-card border border-line bg-card shadow-card">
        <div className="flex items-center gap-3 border-b border-line px-5 py-4">
          <h3 className="text-sm font-semibold text-text">Appointments</h3>
          <span className="ml-auto text-xs text-muted">
            {appointmentsLoading && 'Loading…'}
          </span>
        </div>

        {appointments.length === 0 && !appointmentsLoading ? (
          <p className="px-5 py-6 text-sm text-muted">No appointments for this day.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-line bg-canvas text-left text-[11px] font-semibold uppercase tracking-wide text-muted-2">
                  <th className="px-5 py-3">Time</th>
                  <th className="px-5 py-3">Patient</th>
                  <th className="px-5 py-3">Status</th>
                  <th className="px-5 py-3">Sobreturno</th>
                </tr>
              </thead>
              <tbody>
                {appointments.map((appointment) => (
                  <tr
                    key={appointment.id}
                    className="border-b border-line last:border-b-0 hover:bg-canvas"
                  >
                    <td className="px-5 py-3 font-mono text-xs text-text">
                      {formatTime(appointment.start)}
                    </td>
                    <td className="px-5 py-3">
                      <div className="flex items-center gap-2.5">
                        <div className="grid h-7 w-7 place-items-center rounded-full bg-brand-wash text-[11px] font-bold text-brand-ink">
                          {initials(appointment.patientName)}
                        </div>
                        <span className="font-medium text-text">
                          {appointment.patientName}
                        </span>
                      </div>
                    </td>
                    <td className="px-5 py-3">
                      <span
                        className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold ${statusClass(appointment.status)}`}
                      >
                        <span className="h-1.5 w-1.5 rounded-full bg-current" />
                        {statusLabel(appointment.status)}
                      </span>
                    </td>
                    <td className="px-5 py-3 text-muted">
                      {appointment.isSobreturno ? 'Yes' : 'No'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}

interface KpiTileProps {
  label: string;
  value: number | undefined;
  subline: string | undefined;
}

function KpiTile({ label, value, subline }: KpiTileProps) {
  return (
    <div className="rounded-card border border-line bg-card p-4 shadow-card">
      <span className="text-xs text-muted">{label}</span>
      <b className="mt-1.5 block font-mono text-[28px] leading-none tracking-tight text-text">
        {value === undefined ? '—' : value}
      </b>
      {subline && <small className="mt-1.5 block text-[11px] text-muted-2">{subline}</small>}
    </div>
  );
}
