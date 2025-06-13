using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Authentication;
using Service.DataTransferObjects.Responses.Authentication;

namespace Service.Handlers.Queries.Authentication;

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailRequest, GetUserByEmailResponse>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;


    public GetUserByEmailQueryHandler(ILingualLoopGenericRepository<User> genericRepository, LingualLoopContext context)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<GetUserByEmailResponse> Handle(GetUserByEmailRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Where(u => u.Email == request.Email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return new GetUserByEmailResponse();
        
        return new GetUserByEmailResponse()
        {
            User = user
        };
    }
}