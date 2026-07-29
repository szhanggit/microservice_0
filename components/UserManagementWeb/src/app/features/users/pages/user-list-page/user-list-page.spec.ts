import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { User } from '../../models/user.model';
import { UserListPage } from './user-list-page';

describe('UserListPage', () => {
  let fixture: ComponentFixture<UserListPage>;
  let component: UserListPage;
  let httpMock: HttpTestingController;

  const threeUsers: User[] = [
    { userId: '1', firstName: 'Charlie', lastName: 'Brown', email: 'charlie@example.com' },
    { userId: '2', firstName: 'Alice', lastName: 'Adams', email: 'alice@example.com' },
    { userId: '3', firstName: 'Bob', lastName: 'Zephyr', email: 'bob@example.com' },
  ];

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(UserListPage);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
  });

  /** Triggers the initial (debounced) load and responds with `users`. */
  async function loadWith(users: User[]): Promise<void> {
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(300);
    const req = httpMock.expectOne((r) => r.url === '/users');
    req.flush({ users });
  }

  it('sorts by last name ascending by default', async () => {
    await loadWith(threeUsers);
    expect(component['pageView']().items.map((u) => u.lastName)).toEqual(['Adams', 'Brown', 'Zephyr']);
  });

  it('toggling the same column flips direction; a new column resets to ascending', async () => {
    await loadWith(threeUsers);

    component['toggleSort']('lastName');
    expect(component['sortDirection']()).toBe('desc');
    expect(component['pageView']().items.map((u) => u.lastName)).toEqual(['Zephyr', 'Brown', 'Adams']);

    component['toggleSort']('firstName');
    expect(component['sortColumn']()).toBe('firstName');
    expect(component['sortDirection']()).toBe('asc');
    expect(component['pageView']().items.map((u) => u.firstName)).toEqual(['Alice', 'Bob', 'Charlie']);
  });

  it('paginates results into pages of 10 and clamps navigation at the bounds', async () => {
    const fifteenUsers: User[] = Array.from({ length: 15 }, (_, i) => ({
      userId: `${i}`,
      firstName: `First${i.toString().padStart(2, '0')}`,
      lastName: `Last${i.toString().padStart(2, '0')}`,
      email: `user${i}@example.com`,
    }));

    await loadWith(fifteenUsers);

    expect(component['pageView']().totalPages).toBe(2);
    expect(component['pageView']().items).toHaveLength(10);

    component['goToPage'](2);
    expect(component['pageView']().page).toBe(2);
    expect(component['pageView']().items).toHaveLength(5);

    component['goToPage'](99);
    expect(component['pageView']().page).toBe(2);
  });

  it('debounces search input and requests the gateway with the name filter', async () => {
    await loadWith(threeUsers);

    const inputEvent = { target: { value: 'ali' } } as unknown as Event;
    component['onSearchInput'](inputEvent);
    expect(component['currentPage']()).toBe(1);

    await vi.advanceTimersByTimeAsync(300);
    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.params.get('name')).toBe('ali');
    req.flush({ users: [threeUsers[1]] });

    expect(component['pageView']().items).toEqual([threeUsers[1]]);
  });

  it('opens the form modal in create mode and in edit mode', async () => {
    await loadWith(threeUsers);

    component['openCreateModal']();
    expect(component['isModalOpen']()).toBe(true);
    expect(component['modalUser']()).toBeNull();

    component['openEditModal'](threeUsers[0]);
    expect(component['modalUser']()).toEqual(threeUsers[0]);

    component['closeModal']();
    expect(component['isModalOpen']()).toBe(false);
  });

  it('deletes the pending user on confirm and refreshes the list', async () => {
    await loadWith(threeUsers);

    component['requestDelete'](threeUsers[0]);
    expect(component['userPendingDelete']()).toEqual(threeUsers[0]);

    component['confirmDelete']();
    const deleteReq = httpMock.expectOne(`/users/${threeUsers[0].userId}`);
    expect(deleteReq.request.method).toBe('DELETE');
    deleteReq.flush(null, { status: 204, statusText: 'No Content' });

    expect(component['userPendingDelete']()).toBeNull();

    // Bumping refreshTrigger reaches the pipeline via toObservable(), which
    // pushes through a microtask rather than synchronously.
    await vi.advanceTimersByTimeAsync(0);

    const refreshReq = httpMock.expectOne((r) => r.url === '/users');
    refreshReq.flush({ users: threeUsers.slice(1) });

    expect(component['pageView']().totalCount).toBe(2);
  });

  it('cancelling a delete clears the pending user without calling the API', async () => {
    await loadWith(threeUsers);

    component['requestDelete'](threeUsers[0]);
    component['cancelDelete']();

    expect(component['userPendingDelete']()).toBeNull();
    httpMock.expectNone(`/users/${threeUsers[0].userId}`);
  });
});
