using System.Linq.Expressions;

namespace Postgres.Abstractions;

public interface ILingualLoopGenericRepository<TEntity>
    where TEntity : class
{
    LingualLoopContext GetDbContext();
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> selectFilter);
    Task<IEnumerable<TEntity?>> GetAllUsersAsync(bool trackChanges);
    Task<TEntity?> SingleAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> selectFilter);
    Task<TEntity?> FirstAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> selectFilter);
    void Create(TEntity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    void Update(TEntity entity);
}