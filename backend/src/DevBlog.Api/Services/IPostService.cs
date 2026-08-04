namespace DevBlog.Api.Services;

public interface IPostService
{
    Task<List<PostSummaryDto>> GetAllAsync();
    Task<PostDetailDto?> GetBySlugAsync(string slug);
    Task<CreatePostResult> CreateAsync(CreatePostRequest req, int authorId);
}

public record PostSummaryDto(int Id, string Title, string Slug, string Tags, DateTime PublishedAt, string Author);
public record CommentDto(int Id, string AuthorName, string Body, DateTime CreatedAt);
public record PostDetailDto(int Id, string Title, string Content, string Slug, string Tags,
    DateTime PublishedAt, string Author, IEnumerable<CommentDto> Comments);
public record CreatePostRequest(string Title, string Content, string Slug, string Tags);
public record CreatePostResult(DevBlog.Api.Models.Post? Post, bool SlugConflict);
