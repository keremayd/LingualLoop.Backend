using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Requests.Badge;
using Service.DataTransferObjects.Responses;
using Service.DataTransferObjects.Responses.Badge;

namespace Service.Handlers.Commands;

public class EarnBadgeByIdCommandHandler : IRequestHandler<EarnBadgeByIdRequest, EarnBadgeByIdResponse>
{
    private readonly ILingualLoopGenericRepository<Badge> _badgeRepository;
    private readonly ILingualLoopGenericRepository<UserBadge> _userBadgeRepository;
    private readonly LingualLoopContext _context;

    public EarnBadgeByIdCommandHandler(ILingualLoopGenericRepository<Badge> badgeRepository, ILingualLoopGenericRepository<UserBadge> userBadgeRepository)
    {
        _badgeRepository = badgeRepository;
        _userBadgeRepository = userBadgeRepository;
        _context = userBadgeRepository.GetDbContext();
    }

    public async Task<EarnBadgeByIdResponse> Handle(EarnBadgeByIdRequest request, CancellationToken cancellationToken)
    {
        var badge = await _badgeRepository.FirstAsync(b => b.BadgeId == request.BadgeId,
            badge => new Badge()
            {
                BadgeId = badge.BadgeId,
                BadgeUrl = badge.BadgeUrl,
                BadgeTitle = badge.BadgeTitle,
                BadgeDescription = badge.BadgeDescription,
                CreatedDate = badge.CreatedDate
            }
        );
        
        if (badge == null)
            throw new LingualLoopException(ErrorCode.NoDataFoundInBadge.CreateMessage(request.BadgeId),
                ErrorCode.NoDataFoundInBadge.GetDescription(request.BadgeId), HttpStatusCode.InternalServerError);
        
        var entity = new UserBadge()
        {
            UserId = request.UserId,
            BadgeId = request.BadgeId,
            EarnedDate = DateTime.UtcNow
        };
        
        _userBadgeRepository.Create(entity);
        var response = await _userBadgeRepository.SaveChangesAsync(cancellationToken);
        if (response == 0)
            throw new LingualLoopException(ErrorCode.NoDataCreatedInUserBadge.CreateMessage(request.BadgeId, request.UserId),
                ErrorCode.NoDataCreatedInUserBadge.GetDescription(request.BadgeId, request.UserId), HttpStatusCode.InternalServerError);

        return new EarnBadgeByIdResponse()
        {
            UserId = entity.UserId,
            BadgeId = entity.BadgeId,
            EarnedDate = entity.EarnedDate
        };
    }
}