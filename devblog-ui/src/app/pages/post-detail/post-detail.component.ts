import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PostService, PostDetail } from '../../services/post.service';

@Component({
  selector: 'app-post-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './post-detail.component.html'
})
export class PostDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);
  private postService = inject(PostService);

  post: PostDetail | null = null;
  commentAuthor = '';
  commentBody = '';
  submitted = false;

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.postService.getPost(slug).subscribe(p => {
      this.post = p;
      this.cdr.detectChanges(); //bu satır, değişiklikleri algılamak ve bileşeni güncellemek için ChangeDetectorRef kullanır

    } );
  }

  submitComment() {
    if (!this.post) return;
    this.postService
      .addComment(this.post.slug, { authorName: this.commentAuthor, body: this.commentBody })
      .subscribe(() => {
        this.submitted = true;
        this.commentAuthor = '';
        this.commentBody = '';
        const slug = this.route.snapshot.paramMap.get('slug')!;
        this.postService.getPost(slug).subscribe(p => (this.post = p));
      });
  }
}
