using System.Linq.Expressions;
using KMG.Core.Interfaces;
using KMG.EF.Data;
using Microsoft.EntityFrameworkCore;

namespace KMG.EF.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;

        public BaseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }

        public void Delete(T entity) => _context.Set<T>().Remove(entity);

        public async Task DeleteAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public void DeleteRange(IEnumerable<T> entities) => _context.Set<T>().RemoveRange(entities);

        public async Task<IEnumerable<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();

        public async Task<T?> GetByIdAsync(int id) => await _context.Set<T>().FindAsync(id);

        public T Update(T entity)
        {
            _context.Set<T>().Update(entity);
            return entity;
        }

        public async Task<T?> FindTWithIncludes(int id, string keyName, params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
            return await query.FirstOrDefaultAsync(e => Microsoft.EntityFrameworkCore.EF.Property<int>(e, keyName) == id);
        }

        public async Task<T?> FindTWithQueryIncludes(int id, string keyName, Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            IQueryable<T> query = _context.Set<T>();
            if (includes != null)
            {
                query = includes(query);
            }
            return await query.FirstOrDefaultAsync(e => Microsoft.EntityFrameworkCore.EF.Property<int>(e, keyName) == id);
        }

        public virtual async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria)
        {
            return await _context.Set<T>().Where(criteria).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllWithIncludes(Expression<Func<T, bool>>? criteria, params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = _context.Set<T>();
            if (criteria != null)
            {
                query = query.Where(criteria);
            }
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllWithQueryIncludes(Expression<Func<T, bool>>? criteria = null, params Func<IQueryable<T>, IQueryable<T>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            if (criteria != null)
            {
                query = query.Where(criteria);
            }
            foreach (var include in includes)
            {
                query = include(query);
            }
            return await query.ToListAsync();
        }

        public async Task<T?> FindTWithExpression(params Expression<Func<T, bool>>[] expressions)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var expression in expressions)
            {
                query = query.Where(expression);
            }
            return await query.FirstOrDefaultAsync();
        }

        public IQueryable<T> GetQueryable(Expression<Func<T, bool>>? criteria)
        {
            return criteria == null ? _context.Set<T>() : _context.Set<T>().Where(criteria);
        }
    }
}
