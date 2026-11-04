using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Queries.Karty;

/// <summary>
/// Karty Yolu: üniteleri, kartları ve kullanıcının öğrenme satırlarını okur,
/// adımları <see cref="KartyPathBuilder"/> kurar. Okuma yolu; veri yazmaz.
/// </summary>
public class GetKartyPathQueryHandler
    : IRequestHandler<GetKartyPathRequest, GetKartyPathResponse>
{
    private readonly LingualLoopContext _context;

    public GetKartyPathQueryHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<GetKartyPathResponse> Handle(
        GetKartyPathRequest request,
        CancellationToken cancellationToken)
    {
        // Ünite şemasını da garanti eder.
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var units = await _context.KartyUnits
            .AsNoTracking()
            .OrderBy(unit => unit.Sequence)
            .Select(unit => new PathUnit(
                unit.KartyUnitId, unit.Sequence, unit.Title, unit.EmblemKartyId))
            .ToListAsync(cancellationToken);

        var cards = await _context.Karty
            .AsNoTracking()
            .Where(karty => karty.UnitId != null)
            .Select(karty => new PathCard(
                karty.KartyId,
                karty.UnitId!.Value,
                karty.UnitOrder,
                karty.NounText,
                karty.Article,
                karty.KartyUrl))
            .ToListAsync(cancellationToken);

        var learnings = await _context.UserKartyLearnings
            .AsNoTracking()
            .Where(learning => learning.UserId == request.UserId)
            .Select(learning => new PathLearning(
                learning.KartyId,
                learning.Box,
                learning.DueDate,
                learning.IntroducedDate,
                learning.FirstLearnedDate))
            .ToListAsync(cancellationToken);

        return KartyPathBuilder.Build(units, cards, learnings, DateTime.UtcNow);
    }
}
