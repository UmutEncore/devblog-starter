using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface IPostRepository : IRepository<Post>
{
    Task<bool> ExistsBySlugAsync(string slug);
}
