import type { AppointmentListItemDto } from '../booking/types';

// The list projection is one wire shape; reusing it keeps the new field type-checked
// end to end instead of asserted away with a cast.
export type MyAppointmentListItemDto = AppointmentListItemDto;
