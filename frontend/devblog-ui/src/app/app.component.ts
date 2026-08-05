import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
      <div class="container">
        <a class="navbar-brand" routerLink="/posts">DevBlog</a>
        <div class="navbar-nav">
          <a class="nav-link" routerLink="/posts">Posts</a>
          <a class="nav-link" routerLink="/login">Login</a>
        </div>
      </div>
    </nav>
    <main class="container py-4">
      <router-outlet />
    </main>
  `
})
export class AppComponent {}
