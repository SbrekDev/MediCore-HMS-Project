export const DAYS_OF_WEEK = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
] as const;

export type DayOfWeek = (typeof DAYS_OF_WEEK)[number];

/** Wire shape of a weekly availability block: `"Monday"` and `"HH:mm"` strings. */
export interface AvailabilityBlockDto {
  dayOfWeek: DayOfWeek;
  start: string;
  end: string;
}

/** Wire shape of a per-date exception: full-day when both times are null. */
export interface AgendaExceptionDto {
  id: string;
  date: string;
  start: string | null;
  end: string | null;
  reason: string;
}

export interface AgendaDto {
  professionalId: string;
  maxSobreturnosPerDay: number;
  blocks: AvailabilityBlockDto[];
  exceptions: AgendaExceptionDto[];
}

export interface UpdateAgendaRequest {
  maxSobreturnosPerDay: number;
  blocks: AvailabilityBlockDto[];
}

export interface CreateAgendaExceptionRequest {
  date: string;
  start: string | null;
  end: string | null;
  reason: string;
}
