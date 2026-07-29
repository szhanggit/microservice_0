import { AuthService } from './auth.service';

describe('AuthService', () => {
  it('starts unauthenticated', () => {
    const service = new AuthService();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('login() flips isAuthenticated to true', () => {
    const service = new AuthService();
    service.login();
    expect(service.isAuthenticated()).toBe(true);
  });

  it('logout() flips isAuthenticated back to false', () => {
    const service = new AuthService();
    service.login();
    service.logout();
    expect(service.isAuthenticated()).toBe(false);
  });
});
