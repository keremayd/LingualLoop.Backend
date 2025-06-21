using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
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
        var user = await _genericRepository.FirstAsync(u => u.Id == request.UserId,
            user => new User() { Id = user.Id, UserNickname = user.UserNickname, UserScore = user.UserScore});
        
        if (user is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(request.UserId),
                ErrorCode.NoDataInUsers.GetDescription(request.UserId), HttpStatusCode.BadRequest);
        }
        
        return new GetUserByIdResponse()
        {
            UserId = user.Id,
            UserNickname = user.UserNickname,
            ProfilePhoto = user.ProfilePhoto,
            UserScore = user.UserScore
        };
    }
}