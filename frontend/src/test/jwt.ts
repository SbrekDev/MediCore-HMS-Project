/**
 * Builds an unsigned JWT-shaped string for tests. The signature segment is a
 * placeholder: the frontend only reads the payload for UI shaping, and the
 * server remains the authorization authority.
 */
export function buildJwt(payload: Record<string, unknown>): string {
  const header = { alg: 'HS256', typ: 'JWT' };
  return [
    base64Url(JSON.stringify(header)),
    base64Url(JSON.stringify(payload)),
    'test-signature',
  ].join('.');
}

function base64Url(value: string): string {
  return btoa(value).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}
