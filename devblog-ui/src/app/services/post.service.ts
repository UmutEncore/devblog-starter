import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface PostSummary {
  id: number;
  title: string;
  slug: string;
  tags: string;
  publishedAt: string;
  author: string;
}

export interface PostDetail extends PostSummary {
  content: string;
  comments: Comment[];
}

export interface Comment {
  id: number;
  authorName: string;
  body: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class PostService {
  private http = inject(HttpClient);

  getPosts() {
    return this.http.get<PostSummary[]>(`${environment.apiUrl}/posts`);
  }

  getPost(slug: string) {
    return this.http.get<PostDetail>(`${environment.apiUrl}/posts/${slug}`);
  }

  createPost(data: { title: string; content: string; slug: string; tags: string }) {
    return this.http.post(`${environment.apiUrl}/posts`, data);
  }

  addComment(slug: string, data: { authorName: string; body: string }) {
    return this.http.post(`${environment.apiUrl}/posts/${slug}/comments`, data);
  }
}
