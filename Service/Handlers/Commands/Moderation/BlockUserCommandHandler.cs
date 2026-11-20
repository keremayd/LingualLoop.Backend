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

public class BlockUserCommandHandler : IRequestHandler<BlockUserRequest, BlockUserResponse>
{
    private readonly StelindaContext _context;

    public BlockUserCommandHandler(IStelindaGenericRepository<User> userRepository)
    {
        _context = userRepository.GetDbContext();
    }

    public async Task<BlockUserResponse> Handle(BlockUserRequest request, CancellationToken cancellationToken)
    {
        await UserModerationSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var target = request.BlockedUserId;
        if (string.IsNullOrWhiteSpace(target) || target == request.UserId ||
            !await _context.Users.AnyAsync(u => u.Id == target, cancellationToken))
        {
            throw new StelindaException(ErrorCode.ModerationTargetInvalid.CreateMessage(target),
                ErrorCode.ModerationTargetInvalid.GetDescription(target), HttpStatusCode.BadRequest);
        }

        var existing = await _context.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == request.UserId && b.BlockedId == target, cancellationToken);

        if (request.Block && existing == null)
        {
            _context.UserBlocks.Add(new UserBlock
            {
                BlockerId = request.UserId,
                BlockedId = target,
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (!request.Block && existing != null)
        {
            _context.UserBlocks.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new BlockUserResponse { Blocked = request.Block };
    }
}
