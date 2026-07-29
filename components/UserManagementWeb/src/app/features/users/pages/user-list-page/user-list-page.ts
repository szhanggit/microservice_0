import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { combineLatest, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs/operators';

import { Spinner } from '../../../../shared/components/spinner/spinner';
import { ConfirmDialog } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UserFormModal } from '../../components/user-form-modal/user-form-modal';
import { User } from '../../models/user.model';
import { UserApiService } from '../../services/user-api.service';

type SortColumn = 'firstName' | 'lastName' | 'email';
type SortDirection = 'asc' | 'desc';
type ModalState = { readonly mode: 'closed' } | { readonly mode: 'create' } | { readonly mode: 'edit'; readonly user: User };

const PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;

/**
 * Page two: searchable, sortable, paged user list plus the create/edit
 * modal and delete confirmation dialog.
 *
 * Search is delegated to the gateway's `GET /users?name=` (the only filter
 * it supports); sorting and pagination are done client-side over that
 * result set, since the gateway has no `sort`/`page` query parameters today.
 * That's a deliberate trade-off, not an oversight - revisit if the user
 * list ever grows large enough for client-side paging to be wasteful.
 */
@Component({
  selector: 'app-user-list-page',
  imports: [Spinner, ConfirmDialog, UserFormModal],
  templateUrl: './user-list-page.html',
  styleUrl: './user-list-page.scss',
})
export class UserListPage {
  private readonly userApi = inject(UserApiService);
  private readonly toast = inject(ToastService);
  protected readonly loading = inject(LoadingService);

  protected readonly searchTerm = signal('');
  protected readonly sortColumn = signal<SortColumn>('lastName');
  protected readonly sortDirection = signal<SortDirection>('asc');
  protected readonly currentPage = signal(1);
  protected readonly modalState = signal<ModalState>({ mode: 'closed' });
  protected readonly userPendingDelete = signal<User | null>(null);

  private readonly refreshTrigger = signal(0);

  private readonly users = toSignal(
    combineLatest([
      toObservable(this.searchTerm).pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged()),
      toObservable(this.refreshTrigger),
    ]).pipe(
      switchMap(([term]) =>
        this.userApi.searchUsers(term || undefined).pipe(
          map((response) => response.users),
          catchError(() => of<readonly User[]>([])),
        ),
      ),
    ),
    { initialValue: [] as readonly User[] },
  );

  protected readonly sortedUsers = computed(() => {
    const column = this.sortColumn();
    const direction = this.sortDirection();
    const collator = new Intl.Collator(undefined, { sensitivity: 'base' });

    return [...this.users()].sort((a, b) => {
      const comparison = collator.compare(a[column], b[column]);
      return direction === 'asc' ? comparison : -comparison;
    });
  });

  protected readonly pageView = computed(() => {
    const sorted = this.sortedUsers();
    const totalPages = Math.max(1, Math.ceil(sorted.length / PAGE_SIZE));
    const page = Math.min(Math.max(1, this.currentPage()), totalPages);
    const start = (page - 1) * PAGE_SIZE;

    return {
      items: sorted.slice(start, start + PAGE_SIZE),
      page,
      totalPages,
      totalCount: sorted.length,
    };
  });

  protected onSearchInput(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
    this.currentPage.set(1);
  }

  protected toggleSort(column: SortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDirection.update((direction) => (direction === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
  }

  protected goToPage(page: number): void {
    this.currentPage.set(page);
  }

  protected openCreateModal(): void {
    this.modalState.set({ mode: 'create' });
  }

  protected openEditModal(user: User): void {
    this.modalState.set({ mode: 'edit', user });
  }

  protected closeModal(): void {
    this.modalState.set({ mode: 'closed' });
  }

  protected onUserSaved(): void {
    this.closeModal();
    this.refresh();
  }

  protected requestDelete(user: User): void {
    this.userPendingDelete.set(user);
  }

  protected cancelDelete(): void {
    this.userPendingDelete.set(null);
  }

  protected confirmDelete(): void {
    const user = this.userPendingDelete();
    if (!user) {
      return;
    }

    this.userApi.deleteUser(user.userId).subscribe({
      next: () => {
        this.userPendingDelete.set(null);
        this.toast.success(`${user.firstName} ${user.lastName} was deleted.`);
        this.refresh();
      },
      // Already surfaced as a toast by the global error interceptor; leave the
      // confirmation open so the user can cancel or retry.
      error: () => {},
    });
  }

  private refresh(): void {
    this.refreshTrigger.update((value) => value + 1);
  }

  protected readonly modalUser = computed(() => {
    const state = this.modalState();
    return state.mode === 'edit' ? state.user : null;
  });

  protected readonly isModalOpen = computed(() => this.modalState().mode !== 'closed');
}
