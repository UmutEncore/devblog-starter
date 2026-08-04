using DevBlog.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    private readonly DbSet<T> _set = db.Set<T>();

    public IQueryable<T> Query() => _set.AsQueryable();
    public void Add(T entity) => _set.Add(entity);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
