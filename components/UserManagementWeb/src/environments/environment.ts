/**
 * Development defaults. `apiBaseUrl` is intentionally empty - `ng serve` forwards
 * `/users` to UserManagementGateway via proxy.conf.json, so requests stay same-origin
 * and no CORS configuration is needed on the gateway.
 */
export const environment = {
  production: false,
  apiBaseUrl: '',
};
