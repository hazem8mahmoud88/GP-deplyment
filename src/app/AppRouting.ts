import { MainHomeComponent } from './components/main-home/main-home.component';
import { Route } from "@angular/router";
import { canActivateAuth } from '../app/guards/AuthGuard.guard';

export const AppRoutes: Route[] = [
  { path: '', component: MainHomeComponent },
  { path: 'home', component: MainHomeComponent },
  { path: 'login', loadComponent: () => import('./components/auth/login/login.component').then(mod => mod.LoginComponent) },
  { path: 'dashboard', canActivate: [canActivateAuth], loadComponent: () => import('./components/dashboard/dashboard.component').then(mod => mod.DashboardComponent) },
  { path: 'vote/:id', loadComponent: () => import('./components/voting-process/voting-process.component').then(mod => mod.VotingProcessComponent) },
  { path: 'createOrganizer', loadComponent: () => import('./components/auth/organizer-signup/organizer-signup.component').then(mod => mod.OrganizerSignupComponent) },
  { path: 'addorganizer/:id', loadComponent: () => import('./components/dashboard/election-options/add-organizers/add-organizers.component').then(mod => mod.AddOrganizersComponent) },
  { path: 'election-options/:id', loadComponent: () => import('./components/dashboard/election-options/election-options.component').then(mod => mod.ElectionOptionsComponent) },
  { path: 'election-organizer-options/:id', loadComponent: () => import('./components/dashboard/election-organizer-options/election-organizer-options.component').then(mod => mod.ElectionOrganizerOptionsComponent) },
  { path: 'election-stats/:id', loadComponent: () => import('./components/election-stats/election-stats.component').then(mod => mod.ElectionStatsComponent) }
]
