using System.Linq.Expressions;

namespace KMG.Core.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<T?> GetByIdAsync(int id);
        T Update(T entity);
        Task DeleteAsync(T entity);
        void Delete(T entity);

        Task<T?> FindTWithIncludes(int id, string keyName, params Expression<Func<T, object>>[] includeProperties);
        Task<T?> FindTWithQueryIncludes(int id, string keyName, Func<IQueryable<T>, IQueryable<T>>? includes = null);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria);
        Task<IEnumerable<T>> FindAllWithIncludes(Expression<Func<T, bool>>? criteria, params Expression<Func<T, object>>[] includeProperties);
        Task<IEnumerable<T>> FindAllWithQueryIncludes(Expression<Func<T, bool>>? criteria = null, params Func<IQueryable<T>, IQueryable<T>>[] includes);
        Task<T?> FindTWithExpression(params Expression<Func<T, bool>>[] expressions);
        IQueryable<T> GetQueryable(Expression<Func<T, bool>>? criteria);

        void DeleteRange(IEnumerable<T> entities);
        Task AddRangeAsync(IEnumerable<T> entities);
    }
}
