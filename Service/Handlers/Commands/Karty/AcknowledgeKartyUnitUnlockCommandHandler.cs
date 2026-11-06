using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Commands.Karty;

/// <summary>
/// Ünite açılış kutlaması görüldü: kutlanan ünite açılanla eşitlenir, patika
/// onu bir daha göstermez. Bekleyen kutlama yoksa bir şey değişmez; istek
/// tekrarlansa da sonuç aynı.
/// </summary>
public class AcknowledgeKartyUnitUnlockCommandHandler
    : IRequestHandler<AcknowledgeKartyUnitUnlockRequest, AcknowledgeKartyUnitUnlockResponse>
{
    private readonly LingualLoopContext _context;

    public AcknowledgeKartyUnitUnlockCommandHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<AcknowledgeKartyUnitUnlockResponse> Handle(
        AcknowledgeKartyUnitUnlockRequest request,
        CancellationToken cancellationToken)
    {
        await KartyUnitSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var state = await _context.UserKartyUnitStates
            .FirstOrDefaultAsync(item => item.UserId == request.UserId, cancellationToken);
        if (state is null) return new AcknowledgeKartyUnitUnlockResponse();

        if (state.AnnouncedSequence < state.UnlockedSequence)
        {
            state.AnnouncedSequence = state.UnlockedSequence;
            state.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AcknowledgeKartyUnitUnlockResponse
        {
            AnnouncedSequence = state.AnnouncedSequence,
        };
    }
}
