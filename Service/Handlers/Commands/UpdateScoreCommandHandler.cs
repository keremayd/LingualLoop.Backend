using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;

namespace Service.Handlers.Commands;

public class UpdateScoreCommandHandler : IRequestHandler<UpdateScoreRequest, bool>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;
    
    public UpdateScoreCommandHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<bool> Handle(UpdateScoreRequest request, CancellationToken cancellationToken)
    {
        var user = await _genericRepository.FirstAsync(u => u.UserId == request.UserId,
            user => new User() { UserId = user.UserId, UserName = user.UserName, UserScore = user.UserScore});

        user.UserScore.Score += request.Point;
        
        _genericRepository.Update(user);
        await _genericRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}