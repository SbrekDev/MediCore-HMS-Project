import { z } from 'zod';
import { DAYS_OF_WEEK, type AgendaExceptionDto, type CreateAgendaExceptionRequest, type UpdateAgendaRequest } from '../types';

export const END_BEFORE_START_MESSAGE = 'End time must be after the start time';

const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

/** Minutes since midnight for an `HH:mm` string, or null when the value is not a time. */
export function toMinutes(value: string): number | null {
  const match = TIME_PATTERN.exec(value.trim());
  return match ? Number(match[1]) * 60 + Number(match[2]) : null;
}

/**
 * A block has to end after it starts. An end equal to or earlier than the start is
 * either an empty window or one that would cross midnight, and a weekly template
 * expresses neither.
 */
export function isEndAfterStart(start: string, end: string): boolean {
  const from = toMinutes(start);
  const to = toMinutes(end);

  return from !== null && to !== null && to > from;
}

export const availabilityBlockSchema = z
  .object({
    dayOfWeek: z.enum(DAYS_OF_WEEK),
    start: z.string().regex(TIME_PATTERN, 'Required'),
    end: z.string().regex(TIME_PATTERN, 'Required'),
  })
  .refine((block) => isEndAfterStart(block.start, block.end), {
    message: END_BEFORE_START_MESSAGE,
    path: ['end'],
  });

export const agendaFormSchema = z.object({
  maxSobreturnosPerDay: z
    .number({ error: 'Required' })
    .int('Must be a whole number')
    .min(0, 'Cannot be negative'),
  blocks: z.array(availabilityBlockSchema),
});

export type AgendaFormData = z.infer<typeof agendaFormSchema>;

export const exceptionFormSchema = z
  .object({
    date: z.string().regex(DATE_PATTERN, 'Required'),
    isFullDay: z.boolean(),
    start: z.string(),
    end: z.string(),
    reason: z.string().min(1, 'Required'),
  })
  .refine((values) => values.isFullDay || TIME_PATTERN.test(values.start.trim()), {
    message: 'Required',
    path: ['start'],
  })
  .refine((values) => values.isFullDay || TIME_PATTERN.test(values.end.trim()), {
    message: 'Required',
    path: ['end'],
  })
  .refine((values) => values.isFullDay || isEndAfterStart(values.start, values.end), {
    message: END_BEFORE_START_MESSAGE,
    path: ['end'],
  });

export type ExceptionFormData = z.infer<typeof exceptionFormSchema>;

export function toUpdateAgendaRequest(values: AgendaFormData): UpdateAgendaRequest {
  return {
    maxSobreturnosPerDay: values.maxSobreturnosPerDay,
    blocks: values.blocks.map((block) => ({
      dayOfWeek: block.dayOfWeek,
      start: block.start,
      end: block.end,
    })),
  };
}

/** A full-day exception is the one with no window, which the API spells as null times. */
export function toCreateExceptionRequest(values: ExceptionFormData): CreateAgendaExceptionRequest {
  return {
    date: values.date,
    start: values.isFullDay ? null : values.start,
    end: values.isFullDay ? null : values.end,
    reason: values.reason,
  };
}

export function describeExceptionWindow(exception: AgendaExceptionDto): string {
  return exception.start && exception.end
    ? `${exception.start}–${exception.end}`
    : 'All day';
}
