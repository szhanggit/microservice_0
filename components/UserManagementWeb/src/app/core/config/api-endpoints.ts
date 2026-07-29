import { environment } from '../../../environments/environment';

/**
 * Single source of truth for backend endpoint paths. Every path here maps
 * 1:1 to a route exposed by UserManagementGateway's `UsersController`
 * (components/UserManagementGateway/.../Controllers/UsersController.cs).
 * Adding a new endpoint or moving the gateway to a different origin only
 * requires a change in this file plus `environment.apiBaseUrl`.
 */
export const API_ENDPOINTS = {
  users: `${environment.apiBaseUrl}/users`,
} as const;
