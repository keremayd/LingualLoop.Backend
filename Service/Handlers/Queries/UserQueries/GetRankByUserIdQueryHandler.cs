using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;

namespace Service.Handlers.Queries.UserQueries;

public class GetRankByUserIdQueryHandler : IRequestHandler<GetRankByUserIdRequest, int>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;
    
    public GetRankByUserIdQueryHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<int> Handle(GetRankByUserIdRequest request, CancellationToken cancellationToken)
    {
        var userScore = await _context.UserScores
            .Where(us => us.UserId == request.UserId)
            .Select(us => us.Score)
            .FirstOrDefaultAsync(cancellationToken);
        
        var rank = await _context.UserScores
            .CountAsync(us => us.Score > userScore, cancellationToken);

        return rank + 1;
    }
}