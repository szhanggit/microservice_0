import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';

interface LoginForm {
  username: FormControl<string>;
  password: FormControl<string>;
}

/**
 * No credential validation happens here by design (see `AuthService`) - the
 * form only needs to be non-empty before `login()` is called and the user is
 * sent to the user list.
 */
@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = new FormGroup<LoginForm>({
    username: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.authService.login();
    void this.router.navigateByUrl('/users');
  }
}
