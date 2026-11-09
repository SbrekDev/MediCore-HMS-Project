import { describe, it, expect } from 'vitest';
import { decodeProfessionalId, decodeRoles } from './session';
import { buildJwt } from '../test/jwt';

describe('decodeRoles', () => {
  it('reads a single role claim from the access token payload', () => {
    const token = buildJwt({ role: 'Receptionist' });

    expect(decodeRoles(token)).toEqual(['Receptionist']);
  });

  it('reads multiple role claims serialized as an array', () => {
    const token = buildJwt({ role: ['Admin', 'Receptionist'] });

    expect(decodeRoles(token)).toEqual(['Admin', 'Receptionist']);
  });

  it('returns no roles when the token has no role claim', () => {
    const token = buildJwt({ sub: 'user-1' });

    expect(decodeRoles(token)).toEqual([]);
  });

  it('returns no roles for a malformed token', () => {
    expect(decodeRoles('not-a-jwt')).toEqual([]);
  });
});

describe('decodeProfessionalId', () => {
  it('reads the professional_id claim from the access token payload', () => {
    const token = buildJwt({ sub: 'u-1', role: 'Professional', professional_id: 'prof-1' });

    expect(decodeProfessionalId(token)).toBe('prof-1');
  });

  it('returns null when the token has no professional_id claim', () => {
    const token = buildJwt({ sub: 'u-1', role: 'Professional' });

    expect(decodeProfessionalId(token)).toBeNull();
  });

  it('returns null for a malformed token', () => {
    expect(decodeProfessionalId('not-a-jwt')).toBeNull();
  });
});
