import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { User } from '../../models/user.model';
import { UserFormModal } from './user-form-modal';

describe('UserFormModal', () => {
  let fixture: ComponentFixture<UserFormModal>;
  let component: UserFormModal;
  let httpMock: HttpTestingController;

  const existingUser: User = {
    userId: '22222222-2222-2222-2222-222222222222',
    firstName: 'Grace',
    lastName: 'Hopper',
    email: 'grace@example.com',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(UserFormModal);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('starts in create mode with an empty form', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component['isEditMode']()).toBe(false);
    expect(component['form'].value).toEqual({ firstName: '', lastName: '', email: '' });
  });

  it('pre-fills the form and switches to edit mode when given a user', async () => {
    fixture.componentRef.setInput('user', existingUser);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component['isEditMode']()).toBe(true);
    expect(component['form'].value).toEqual({
      firstName: existingUser.firstName,
      lastName: existingUser.lastName,
      email: existingUser.email,
    });
  });

  it('does not submit when the form is invalid', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component['onSubmit']();

    httpMock.expectNone(() => true);
    expect(component['form'].controls.firstName.touched).toBe(true);
  });

  it('POSTs a new user and emits saved on success in create mode', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    let savedUser: User | undefined;
    component.saved.subscribe((user) => (savedUser = user));

    component['form'].setValue({ firstName: 'Ada', lastName: 'Lovelace', email: 'ada@example.com' });
    component['onSubmit']();

    const req = httpMock.expectOne('/users');
    expect(req.request.method).toBe('POST');
    req.flush({ userId: '333', ...component['form'].getRawValue() }, { status: 201, statusText: 'Created' });

    expect(savedUser?.email).toBe('ada@example.com');
  });

  it('PUTs the updated user and emits saved on success in edit mode', async () => {
    fixture.componentRef.setInput('user', existingUser);
    fixture.detectChanges();
    await fixture.whenStable();

    let savedUser: User | undefined;
    component.saved.subscribe((user) => (savedUser = user));

    component['form'].controls.lastName.setValue('Murray');
    component['onSubmit']();

    const req = httpMock.expectOne(`/users/${existingUser.userId}`);
    expect(req.request.method).toBe('PUT');
    req.flush({ ...existingUser, lastName: 'Murray' });

    expect(savedUser?.lastName).toBe('Murray');
  });
});
