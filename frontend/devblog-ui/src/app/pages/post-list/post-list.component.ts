import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PostService, PostSummary } from '../../services/post.service';

@Component({
  selector: 'app-post-list',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './post-list.component.html'
})
export class PostListComponent implements OnInit {
  private postService = inject(PostService);
    private cdr = inject(ChangeDetectorRef);
  posts: PostSummary[] = [];

  ngOnInit() {
    this.postService.getPosts().subscribe((posts) => {
      this.posts = posts;
      this.cdr.detectChanges(); //bu satır, değişiklikleri algılamak ve bileşeni güncellemek için ChangeDetectorRef kullanır
    });
  }
}
