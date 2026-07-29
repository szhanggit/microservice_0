import { UrlTree, provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
  });

  function runGuard() {
    return TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
  }

  it('allows navigation when authenticated', () => {
    TestBed.inject(AuthService).login();
    expect(runGuard()).toBe(true);
  });

  it('redirects to /login when not authenticated', () => {
    const result = runGuard();
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/login');
  });
});
