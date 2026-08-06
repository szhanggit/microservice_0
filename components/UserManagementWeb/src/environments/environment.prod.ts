/**
 * Production defaults. The app is served from CloudFront on
 * usermgn.ekslab.xyz (see terraform/modules/frontend-cdn), a different
 * origin than the gateway's own domain (microservice0.ekslab.xyz, still
 * served directly by the ALB) - so this is a genuine cross-origin call, and
 * UserManagementGateway must have CORS enabled for this origin (see its
 * Program.cs).
 */
export const environment = {
  production: true,
  apiBaseUrl: 'https://microservice0.ekslab.xyz',
};
