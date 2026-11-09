import { useSyncExternalStore } from 'react';

export interface Session {
  accessToken: string;
  userId: string;
  email: string;
  roles: string[];
}

let session: Session | null = null;
const listeners = new Set<() => void>();

function emit(): void {
  for (const listener of listeners) {
    listener();
  }
}

export const sessionStore = {
  get: (): Session | null => session,
  set: (next: Session): void => {
    session = next;
    emit();
  },
  clear: (): void => {
    session = null;
    emit();
  },
  subscribe: (listener: () => void): (() => void) => {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};

export function useSession(): Session | null {
  return useSyncExternalStore(
    sessionStore.subscribe,
    sessionStore.get,
    sessionStore.get
  );
}

/**
 * Reads role claims from the access token payload so the UI can shape itself.
 * The server stays authoritative: this never grants access, it only hides UI.
 */
export function decodeRoles(accessToken: string): string[] {
  const payload = decodePayload(accessToken);
  if (!payload) {
    return [];
  }

  const claim = payload.role ?? payload.roles;
  if (typeof claim === 'string') {
    return [claim];
  }
  if (Array.isArray(claim)) {
    return claim.filter((value): value is string => typeof value === 'string');
  }
  return [];
}

/**
 * Reads the professional_id claim from the access token payload so the UI can
 * decide whether to offer the "My appointments" view. The server stays
 * authoritative: this only shapes navigation, it never grants access.
 */
export function decodeProfessionalId(accessToken: string): string | null {
  const payload = decodePayload(accessToken);
  if (!payload) {
    return null;
  }

  const claim = payload.professional_id;
  if (typeof claim === 'string' && claim.length > 0) {
    return claim;
  }
  return null;
}

function decodePayload(accessToken: string): Record<string, unknown> | null {
  const segments = accessToken.split('.');
  if (segments.length < 2) {
    return null;
  }

  try {
    const normalized = segments[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(
      normalized.length + ((4 - (normalized.length % 4)) % 4),
      '='
    );
    const parsed: unknown = JSON.parse(atob(padded));
    return parsed && typeof parsed === 'object' ? (parsed as Record<string, unknown>) : null;
  } catch {
    return null;
  }
}
