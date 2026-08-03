import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'posts', pathMatch: 'full' },
  {
    path: 'posts',
    loadComponent: () =>
      import('./pages/post-list/post-list.component').then(m => m.PostListComponent)
  },
  {
    path: 'posts/:slug',
    loadComponent: () =>
      import('./pages/post-detail/post-detail.component').then(m => m.PostDetailComponent)
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then(m => m.LoginComponent)
  }
];
