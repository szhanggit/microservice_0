import { Routes } from '@angular/router';

import { authGuard } from './features/authentication/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    loadChildren: () => import('./features/authentication/routes').then((m) => m.AUTHENTICATION_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layouts/main-layout/main-layout').then((m) => m.MainLayout),
    children: [
      {
        path: 'users',
        loadChildren: () => import('./features/users/routes').then((m) => m.USERS_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
