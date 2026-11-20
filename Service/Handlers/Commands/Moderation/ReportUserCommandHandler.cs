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
using Service.Helpers;

namespace Service.Handlers.Commands;

public class ReportUserCommandHandler : IRequestHandler<ReportUserRequest, ReportUserResponse>
{
    private static readonly HashSet<string> Reasons = new() { "name", "photo", "other" };
    private static readonly HashSet<string> Contexts = new() { "league" };

    private readonly StelindaContext _context;

    public ReportUserCommandHandler(IStelindaGenericRepository<User> userRepository)
    {
        _context = userRepository.GetDbContext();
    }

    public async Task<ReportUserResponse> Handle(ReportUserRequest request, CancellationToken cancellationToken)
    {
        await UserModerationSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var target = request.ReportedUserId;
        if (string.IsNullOrWhiteSpace(target) || target == request.UserId ||
            !await _context.Users.AnyAsync(u => u.Id == target, cancellationToken))
        {
            throw new StelindaException(ErrorCode.ModerationTargetInvalid.CreateMessage(target),
                ErrorCode.ModerationTargetInvalid.GetDescription(target), HttpStatusCode.BadRequest);
        }

        if (!Reasons.Contains(request.Reason) || !Contexts.Contains(request.Context))
        {
            var given = $"{request.Reason}/{request.Context}";
            throw new StelindaException(ErrorCode.ReportReasonInvalid.CreateMessage(given),
                ErrorCode.ReportReasonInvalid.GetDescription(given), HttpStatusCode.BadRequest);
        }

        _context.UserReports.Add(new UserReport
        {
            ReporterId = request.UserId,
            ReportedId = target,
            Reason = request.Reason,
            Context = request.Context,
            CreatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new ReportUserResponse { Reported = true };
    }
}
