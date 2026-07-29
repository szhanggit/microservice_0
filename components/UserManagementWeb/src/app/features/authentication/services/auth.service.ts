import { Injectable, signal } from '@angular/core';

/**
 * Gates navigation to the users feature. Per the current requirements there
 * is no real credential check yet - `login()` unconditionally succeeds - so
 * this deliberately holds state in memory only (no token, no persistence).
 * A page refresh drops the session back to the login page, which is
 * acceptable for now; wiring this up to UserManagementGateway/an identity
 * provider later only means replacing `login()`'s body, not this service's
 * shape or the route guard that depends on it.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _isAuthenticated = signal(false);
  readonly isAuthenticated = this._isAuthenticated.asReadonly();

  login(): void {
    this._isAuthenticated.set(true);
  }

  logout(): void {
    this._isAuthenticated.set(false);
  }
}
