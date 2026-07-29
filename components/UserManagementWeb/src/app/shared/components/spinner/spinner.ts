import { Component, input } from '@angular/core';

/**
 * Bootstrap spinner. `overlay` mode fixes it to the viewport with a dimmed
 * backdrop for global "request in flight" feedback (see `App`'s template,
 * driven by `LoadingService.isLoading()`); non-overlay mode is an inline
 * spinner for in-place use (e.g. a busy button).
 */
@Component({
  selector: 'app-spinner',
  templateUrl: './spinner.html',
  styleUrl: './spinner.scss',
})
export class Spinner {
  readonly overlay = input(false);
  readonly label = input('Loading…');
}
