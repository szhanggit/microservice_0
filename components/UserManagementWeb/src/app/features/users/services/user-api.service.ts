import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_ENDPOINTS } from '../../../core/config/api-endpoints';
import { CreateUserRequest, UpdateUserRequest, User, UserListResponse } from '../models/user.model';

/**
 * Thin HTTP client for `UsersController` on UserManagementGateway. No
 * business logic lives here - callers (currently only `UserListPage` and
 * `UserFormModal`) own loading/error UI concerns via the global interceptors.
 *
 * Note: the gateway's `GET /users` only accepts an optional `name` filter -
 * there is no server-side paging or sorting today, so those are handled
 * client-side against the full filtered result set (see `UserListPage`).
 */
@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);

  searchUsers(name?: string): Observable<UserListResponse> {
    let params = new HttpParams();
    if (name) {
      params = params.set('name', name);
    }
    return this.http.get<UserListResponse>(API_ENDPOINTS.users, { params });
  }

  getUserById(userId: string): Observable<User> {
    return this.http.get<User>(`${API_ENDPOINTS.users}/${userId}`);
  }

  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(API_ENDPOINTS.users, request);
  }

  updateUser(userId: string, request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${API_ENDPOINTS.users}/${userId}`, request);
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${API_ENDPOINTS.users}/${userId}`);
  }
}
