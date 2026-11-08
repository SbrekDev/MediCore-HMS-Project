import { describe, it, expect } from 'vitest';
import { decodeRoles } from './session';
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
