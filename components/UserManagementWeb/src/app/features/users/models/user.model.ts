/** Mirrors `Shared.Contracts.Responses.UserResponse` (libs/Shared.Contracts). */
export interface User {
  readonly userId: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
}

/** Mirrors `Shared.Contracts.Responses.UserListResponse`. */
export interface UserListResponse {
  readonly users: readonly User[];
}

/** Mirrors `Shared.Contracts.Requests.CreateUserRequest`. */
export interface CreateUserRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
}

/** Mirrors `Shared.Contracts.Requests.UpdateUserRequest`. */
export interface UpdateUserRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
}
