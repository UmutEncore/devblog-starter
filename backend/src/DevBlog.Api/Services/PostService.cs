using DevBlog.Api.Models;
using DevBlog.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Services;

public class PostService(IPostRepository posts) : IPostService
{
    public async Task<List<PostSummaryDto>> GetAllAsync() =>
        await posts.Query()
            .Include(p => p.Author)
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new PostSummaryDto(p.Id, p.Title, p.Slug, p.Tags, p.PublishedAt, p.Author.Username, p.Comments.Count()))
            .ToListAsync();

    public async Task<PostDetailDto?> GetBySlugAsync(string slug) =>
        await posts.Query()
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Where(p => p.Slug == slug)
            .Select(p => new PostDetailDto(
                p.Id, p.Title, p.Content, p.Slug, p.Tags, p.PublishedAt, p.Author.Username,
                p.Comments.OrderBy(c => c.CreatedAt)
                    .Select(c => new CommentDto(c.Id, c.AuthorName, c.Body, c.CreatedAt)),
                p.Comments.Count()))
            .FirstOrDefaultAsync();

    public async Task<CreatePostResult> CreateAsync(CreatePostRequest req, int authorId)
    {
        // Case-insensitive match relies on Post.Slug's NOCASE collation (AppDbContext.OnModelCreating).
        // No DbUpdateException backstop for the race window here by design — low-traffic, authenticated-only
        // write path; the unique index is the defense-in-depth backstop if this check is ever bypassed.
        if (await posts.ExistsBySlugAsync(req.Slug))
            return new CreatePostResult(null, SlugConflict: true);

        var post = new Post
        {
            Title = req.Title,
            Content = req.Content,
            Slug = req.Slug,
            Tags = req.Tags,
            PublishedAt = DateTime.UtcNow,
            AuthorId = authorId
        };

        posts.Add(post);
        await posts.SaveChangesAsync();

        return new CreatePostResult(post, SlugConflict: false);
    }
}
