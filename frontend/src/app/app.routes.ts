import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'auth',
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent), data: { mode: 'login' } },
      { path: 'register', loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent), data: { mode: 'register' } },
      { path: 'forgot-password', loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent), data: { mode: 'forgot' } },
      { path: 'reset-password', loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent), data: { mode: 'reset' } }
    ]
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent) },
      { path: 'itineraries', loadComponent: () => import('./features/itineraries/itinerary-list.component').then((m) => m.ItineraryListComponent) },
      { path: 'itineraries/:id', loadComponent: () => import('./features/itineraries/itinerary-detail.component').then((m) => m.ItineraryDetailComponent) },
      { path: 'hotels', loadComponent: () => import('./features/hotels/hotels.component').then((m) => m.HotelsComponent) },
      { path: 'flights', loadComponent: () => import('./features/flights/flights.component').then((m) => m.FlightsComponent) },
      { path: 'reports', loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent) },
      {
        path: 'admin',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/admin/admin-users.component').then((m) => m.AdminUsersComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
