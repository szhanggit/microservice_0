/**
 * Production defaults. Assumes this app is served from behind the same
 * ingress/reverse proxy as UserManagementGateway, so `/users` resolves without
 * a cross-origin hop. If the gateway lives on a different origin in your
 * deployment, point `apiBaseUrl` at it and enable CORS on the gateway.
 */
export const environment = {
  production: true,
  apiBaseUrl: '',
};
