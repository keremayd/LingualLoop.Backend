using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Queries;

public class GetAllUsersQueryHandler: IRequestHandler<GetAllUsersRequest, List<GetAllUsersResponse>>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;

    public GetAllUsersQueryHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<List<GetAllUsersResponse>> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var users = await _genericRepository.GetAllUsersAsync(true);
        
        //TODO burası mapper olacak
        var userResponses = users.Select(u => new GetAllUsersResponse
        {
            UserId = u.UserId,
            UserNickname = u.UserNickname,
        }).ToList();

        return userResponses;
    }
}