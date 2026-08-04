namespace DevBlog.Api.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    void Add(T entity);
    Task SaveChangesAsync();
}
