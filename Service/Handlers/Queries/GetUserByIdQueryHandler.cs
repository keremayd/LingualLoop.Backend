using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Queries;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdRequest, GetUserByIdResponse>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;

    public GetUserByIdQueryHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<GetUserByIdResponse> Handle(GetUserByIdRequest request, CancellationToken cancellationToken)
    {
        var user = await _genericRepository.FirstAsync(u => u.UserId == request.UserId,
            user => new User() { UserId = user.UserId, UserName = user.UserName, UserScore = user.UserScore});

        return new GetUserByIdResponse()
        {
            UserId = user!.UserId,
            UserName = user.UserName,
            UserScore = user.UserScore
        };
    }
}