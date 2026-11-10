/**
 * The one status vocabulary for appointment lists. Three screens render the same five
 * statuses; a copy per feature is how they drift apart.
 */
export function statusTone(status: string): 'brand' | 'ok' | 'warn' | 'danger' | 'neutral' {
  switch (status) {
    case 'Confirmed':
      return 'brand';
    case 'Completed':
      return 'ok';
    case 'NoShow':
      return 'danger';
    case 'Cancelled':
      return 'neutral';
    default:
      return 'neutral';
  }
}

export function statusLabel(status: string): string {
  return status === 'NoShow' ? 'No-show' : status;
}
