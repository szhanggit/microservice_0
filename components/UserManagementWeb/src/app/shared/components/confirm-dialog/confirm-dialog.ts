import { Component, input, output } from '@angular/core';

import { ModalShell } from '../../ui/modal/modal-shell';

/**
 * Reusable yes/no confirmation modal. `UserListPage` uses it to gate delete
 * requests behind an explicit confirmation, but the component itself knows
 * nothing about users - it only knows a message and two outcomes.
 */
@Component({
  selector: 'app-confirm-dialog',
  imports: [ModalShell],
  templateUrl: './confirm-dialog.html',
})
export class ConfirmDialog {
  readonly title = input<string>('Please confirm');
  readonly message = input.required<string>();
  readonly confirmLabel = input<string>('Confirm');
  readonly cancelLabel = input<string>('Cancel');

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
