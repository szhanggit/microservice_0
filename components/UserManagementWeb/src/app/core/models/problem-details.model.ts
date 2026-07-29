/**
 * Mirrors the JSON body written by `GlobalExceptionHandler` in `Shared.Common`
 * (libs/Shared.Common/Middleware/GlobalExceptionHandler.cs) on every non-2xx
 * response from UserManagementGateway. `errors` is only present for 400
 * responses raised from a `ValidationException` and is a flat list of
 * human-readable messages, not a per-field map.
 */
export interface ProblemDetails {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  readonly instance?: string;
  readonly errors?: readonly string[];
}
