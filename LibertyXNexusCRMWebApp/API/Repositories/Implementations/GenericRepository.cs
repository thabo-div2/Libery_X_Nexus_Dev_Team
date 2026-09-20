using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace API.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _dbSet = _dbContextFactory.CreateDbContext().Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.AsNoTracking().ToListAsync();

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

        public virtual async Task<T> AddAsync(T entity)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            if (entity is null) throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            if (entity is null) throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await context.SaveChangesAsync();
        }
        
        public virtual async Task DeleteAsync(int id)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var entity = await _dbSet.FindAsync(id);

            if (entity is null) throw new KeyNotFoundException($"{typeof(T).Name} with id {id} was not found.");

            _dbSet.Remove(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task<bool> ExistsAsync(int id) => await _dbSet.FindAsync(id) is not null;

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) 
            => predicate is null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
    }
}
