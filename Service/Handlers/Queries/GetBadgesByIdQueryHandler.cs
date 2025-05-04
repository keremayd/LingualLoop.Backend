using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Badge;
using Service.DataTransferObjects.Responses.Badge;

namespace Service.Handlers.Queries;

public class GetBadgesByIdQueryHandler : IRequestHandler<GetBadgesByIdRequest, List<GetBadgesByIdResponse>>
{
    private readonly ILingualLoopGenericRepository<UserBadge> _userBadgeRepository;

    public GetBadgesByIdQueryHandler(ILingualLoopGenericRepository<UserBadge> userBadgeRepository)
    {
        _userBadgeRepository = userBadgeRepository;
    }

    public async Task<List<GetBadgesByIdResponse>> Handle(GetBadgesByIdRequest request, CancellationToken cancellationToken)
    {
        var badgeList = await _userBadgeRepository.GetListAsync(u => u.UserId == request.UserId,
            badge => new UserBadge()
            {
                UserBadgeId = badge.UserBadgeId,
                UserId = badge.UserId,
                BadgeId = badge.BadgeId,
                EarnedDate = badge.EarnedDate,
                Badge = badge.Badge
            }
        );

        if (badgeList is null || !badgeList.Any())
            throw new LingualLoopException(ErrorCode.NoDataFoundInUserBadge.CreateMessage(request.UserId),
                ErrorCode.NoDataFoundInUserBadge.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        return badgeList.Select(b => new GetBadgesByIdResponse
        {
            UserBadgeId = b.UserBadgeId,
            UserId = b.UserId,
            EarnedDate = b.EarnedDate,
            Badge = b.Badge!
        }).ToList();
    }
}