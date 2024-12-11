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

public class GetLivesByIdQueryHandler : IRequestHandler<GetLivesByIdRequest, GetLivesByIdResponse>
{
    private readonly ILingualLoopGenericRepository<UserLives> _userLivesRepository;

    public GetLivesByIdQueryHandler(ILingualLoopGenericRepository<UserLives> userLivesRepository)
    {
        _userLivesRepository = userLivesRepository;
    }

    public async Task<GetLivesByIdResponse> Handle(GetLivesByIdRequest request, CancellationToken cancellationToken)
    {
        var userLives = await _userLivesRepository.FirstAsync(u => u.UserId == request.UserId,
            user => new UserLives() { UserLivesId = user.UserLivesId, UserId = user.UserId, Lives = user.Lives, MaxLives = user.MaxLives, LastLivesResetTime = user.LastLivesResetTime});
        if (userLives is null)
            throw new LingualLoopException(ErrorCode.NoDataFoundInUserLives.CreateMessage(request.UserId),
                ErrorCode.NoDataFoundInUserLives.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        return new GetLivesByIdResponse()
        {
            UserId = userLives.UserId,
            Lives = userLives.Lives,
            MaxLives = userLives.MaxLives,
            LastLivesResetTime = userLives.LastLivesResetTime
        };
    }
}