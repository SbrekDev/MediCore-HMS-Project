// Mirrors the server policy matrix: Admin+Receptionist for patients/booking,
// Admin for the professional catalog and the agenda (every agenda write is AdminOnly).
// Kept in one module so the route guards and the landing redirect cannot drift apart.
export const RECEPTION_STAFF = ['Admin', 'Receptionist'] as const;
export const CLINICAL_STAFF = ['Admin', 'Receptionist', 'Professional'] as const;
export const ADMIN_ONLY = ['Admin'] as const;
