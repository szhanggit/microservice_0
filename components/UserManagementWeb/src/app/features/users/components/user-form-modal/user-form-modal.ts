import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { ModalShell } from '../../../../shared/ui/modal/modal-shell';
import { USER_VALIDATION } from '../../models/user-validation.constants';
import { User } from '../../models/user.model';
import { UserApiService } from '../../services/user-api.service';
import { ToastService } from '../../../../core/services/toast.service';

interface UserForm {
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  email: FormControl<string>;
}

/**
 * Create/edit form, shown as a modal per the "Create New User"/"Edit"
 * buttons on `UserListPage`. `user` being non-null switches the component
 * into edit mode (pre-fills the form, calls `updateUser`); null means create.
 */
@Component({
  selector: 'app-user-form-modal',
  imports: [ReactiveFormsModule, ModalShell],
  templateUrl: './user-form-modal.html',
})
export class UserFormModal {
  private readonly userApi = inject(UserApiService);
  private readonly toast = inject(ToastService);

  readonly user = input<User | null>(null);
  readonly saved = output<User>();
  readonly closed = output<void>();

  protected readonly validation = USER_VALIDATION;
  protected readonly isEditMode = computed(() => this.user() !== null);
  protected readonly isSaving = signal(false);

  protected readonly form = new FormGroup<UserForm>({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(USER_VALIDATION.firstNameMaxLength)],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(USER_VALIDATION.lastNameMaxLength)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(USER_VALIDATION.emailMaxLength)],
    }),
  });

  constructor() {
    effect(() => {
      const user = this.user();
      this.form.reset({
        firstName: user?.firstName ?? '',
        lastName: user?.lastName ?? '',
        email: user?.email ?? '',
      });
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const value = this.form.getRawValue();
    const currentUser = this.user();
    const request$ = currentUser
      ? this.userApi.updateUser(currentUser.userId, value)
      : this.userApi.createUser(value);

    request$.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: (savedUser) => {
        this.toast.success(`User ${currentUser ? 'updated' : 'created'} successfully.`);
        this.saved.emit(savedUser);
      },
      // Already surfaced as a toast by the global error interceptor; keep the
      // modal open so the user can correct input (e.g. a duplicate email) and retry.
      error: () => {},
    });
  }
}
