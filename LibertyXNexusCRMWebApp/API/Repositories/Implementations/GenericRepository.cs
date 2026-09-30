using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace API.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public GenericRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Set<T>().FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Set<T>().AsNoTracking().ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            using var context = await _dbContextFactory.CreateDbContextAsync();
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(int id)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var entity = await context.Set<T>().FindAsync(id);

            if (entity is null)
                throw new KeyNotFoundException($"{typeof(T).Name} with id {id} was not found.");

            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Set<T>().FindAsync(id) is not null;
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return predicate is null
                ? await context.Set<T>().CountAsync()
                : await context.Set<T>().CountAsync(predicate);
        }
    }
}
