using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserRequest, bool>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;

    public CreateUserCommandHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }

    public async Task<bool> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User()
        {
            UserName = request.UserName,
            UserScore = {}
        };

        _genericRepository.Create(user);
        var response = await _genericRepository.SaveChangesAsync(cancellationToken);
        if (response == 0)
        {
            return false;
        }

        return true;
    }
}