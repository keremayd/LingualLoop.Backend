using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Postgres.Abstractions;
using Postgres.Models;

namespace Postgres;

public class LingualLoopGenericRepository<TEntity> : ILingualLoopGenericRepository<TEntity>
    where TEntity : class
{
    private readonly LingualLoopContext _context;
    private readonly UserManager<User> _userManager;

    public LingualLoopGenericRepository(LingualLoopContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public LingualLoopContext GetDbContext()
    {
        return _context;
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> selectFilter)
    {
        return await _context
            .Set<TEntity>()
            .AsNoTracking()
            .Where(filter)
            .Select(selectFilter)
            .ToListAsync();
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