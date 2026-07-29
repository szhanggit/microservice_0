import { Component, HostListener, input, output } from '@angular/core';

let nextTitleId = 0;

/**
 * Generic Bootstrap-styled modal shell. Deliberately does not use
 * Bootstrap's own JS bundle (`bootstrap.bundle.js`/`data-bs-toggle`) -
 * visibility is owned by whichever parent conditionally renders this
 * component with `@if`, keeping the whole thing declarative and testable
 * without a second, imperative state machine to keep in sync.
 *
 * Usage:
 * ```html
 * @if (isOpen()) {
 *   <app-modal-shell [title]="'Edit user'" (closed)="isOpen.set(false)">
 *     ...form markup...
 *   </app-modal-shell>
 * }
 * ```
 */
@Component({
  selector: 'app-modal-shell',
  templateUrl: './modal-shell.html',
  styleUrl: './modal-shell.scss',
})
export class ModalShell {
  readonly title = input.required<string>();
  readonly size = input<'sm' | 'lg' | 'xl' | undefined>(undefined);
  readonly closed = output<void>();

  protected readonly titleId = `modal-title-${nextTitleId++}`;

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closed.emit();
  }

  protected onBackdropClick(): void {
    this.closed.emit();
  }
}
