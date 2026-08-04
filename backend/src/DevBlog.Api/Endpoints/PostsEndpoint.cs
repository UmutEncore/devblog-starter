using System.Security.Claims;
using DevBlog.Api.Services;

namespace DevBlog.Api.Endpoints;

public static class PostsEndpoint
{
    public static void Map(WebApplication app)
    {
        // TODO: add pagination — şu an tüm postlar dönüyor
        app.MapGet("/posts", async (IPostService posts) =>
            Results.Ok(await posts.GetAllAsync()));

        app.MapGet("/posts/{slug}", async (string slug, IPostService posts) =>
        {
            var post = await posts.GetBySlugAsync(slug);
            return post is null ? Results.NotFound() : Results.Ok(post);
        });

        app.MapPost("/posts", async (CreatePostRequest req, IPostService posts, ClaimsPrincipal user) =>
        {
            var authorId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await posts.CreateAsync(req, authorId);

            if (result.SlugConflict)
                return Results.Conflict(new { message = $"A post with slug '{req.Slug}' already exists." });

            return Results.Created($"/posts/{result.Post!.Slug}", new { result.Post.Id, result.Post.Slug });
        }).RequireAuthorization();
    }
}
