using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class PostRepository(AppDbContext db) : Repository<Post>(db), IPostRepository
{
    public Task<bool> ExistsBySlugAsync(string slug) =>
        Query().AnyAsync(p => p.Slug == slug);
}
