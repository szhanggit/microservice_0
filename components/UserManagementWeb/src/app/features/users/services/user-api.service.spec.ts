import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { User, UserListResponse } from '../models/user.model';
import { UserApiService } from './user-api.service';

describe('UserApiService', () => {
  let service: UserApiService;
  let httpMock: HttpTestingController;

  const sampleUser: User = {
    userId: '11111111-1111-1111-1111-111111111111',
    firstName: 'Ada',
    lastName: 'Lovelace',
    email: 'ada@example.com',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UserApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('searchUsers requests /users with no params when name is omitted', () => {
    const response: UserListResponse = { users: [sampleUser] };

    service.searchUsers().subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('name')).toBe(false);
    req.flush(response);
  });

  it('searchUsers sends the name filter as a query param', () => {
    service.searchUsers('Ada').subscribe();

    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.params.get('name')).toBe('Ada');
    req.flush({ users: [] } satisfies UserListResponse);
  });

  it('getUserById requests /users/{id}', () => {
    service.getUserById(sampleUser.userId).subscribe((result) => {
      expect(result).toEqual(sampleUser);
    });

    const req = httpMock.expectOne(`/users/${sampleUser.userId}`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleUser);
  });

  it('createUser POSTs to /users with the request body', () => {
    const request = { firstName: 'Ada', lastName: 'Lovelace', email: 'ada@example.com' };

    service.createUser(request).subscribe((result) => {
      expect(result).toEqual(sampleUser);
    });

    const req = httpMock.expectOne('/users');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sampleUser, { status: 201, statusText: 'Created' });
  });

  it('updateUser PUTs to /users/{id} with the request body', () => {
    const request = { firstName: 'Ada', lastName: 'Byron', email: 'ada@example.com' };

    service.updateUser(sampleUser.userId, request).subscribe();

    const req = httpMock.expectOne(`/users/${sampleUser.userId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleUser, lastName: 'Byron' });
  });

  it('deleteUser DELETEs /users/{id}', () => {
    service.deleteUser(sampleUser.userId).subscribe();

    const req = httpMock.expectOne(`/users/${sampleUser.userId}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
