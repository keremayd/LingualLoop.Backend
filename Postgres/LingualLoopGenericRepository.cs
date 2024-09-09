using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Postgres.Abstractions;

namespace Postgres;

public class LingualLoopGenericRepository<TEntity> : ILingualLoopGenericRepository<TEntity>
    where TEntity : class
{
    private readonly LingualLoopContext _context;
    
    public LingualLoopGenericRepository(LingualLoopContext context)
    {
        _context = context;
    }

    public LingualLoopContext GetDbContext()
    {
        return _context;
    }

    public async Task<IEnumerable<TEntity?>> GetAllUsersAsync(bool trackChanges)
    {
        return await _context
            .Set<TEntity>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TEntity?> SingleAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> selectFilter)
    {
        return await _context
            .Set<TEntity>()
            .AsNoTracking()
            .Select(selectFilter)
            .SingleOrDefaultAsync(filter);
    }

    public async Task<TEntity?> FirstAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> selectFilter)
    {
        return await _context
            .Set<TEntity>()
            .AsNoTracking()
            .Select(selectFilter)
            .FirstOrDefaultAsync(filter);
    }

    public void Create(TEntity entity)
    {
        _context.Set<TEntity>().Add(entity);
    }

    public void Update(TEntity entity)
    {
        _context
            .Set<TEntity>()
            .Update(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}