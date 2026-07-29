import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';

import { AuthService } from '../../features/authentication/services/auth.service';

/**
 * Shell for every route behind `authGuard`. Login intentionally has no
 * layout of its own (see `app.routes.ts`) since it needs no nav chrome.
 */
@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet],
  templateUrl: './main-layout.html',
})
export class MainLayout {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected onLogout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }
}
