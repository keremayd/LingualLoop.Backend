using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;
using KartyModel = Postgres.Models.Karty;

namespace Service.Handlers.Queries.Karty;

public class GetKartyMeaningsQueryHandler
    : IRequestHandler<GetKartyMeaningsRequest, GetKartyMeaningsResponse>
{
    private readonly StelindaContext _context;

    public GetKartyMeaningsQueryHandler(IStelindaGenericRepository<KartyModel> kartyRepository)
    {
        _context = kartyRepository.GetDbContext();
    }

    public async Task<GetKartyMeaningsResponse> Handle(
        GetKartyMeaningsRequest request,
        CancellationToken cancellationToken)
    {
        await KartyTranslationSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var locale = KartyTranslationWriter.RequireLocale(request.Locale);

        var items = await (
                from karty in _context.Karty.AsNoTracking()
                join unit in _context.KartyUnits on karty.UnitId equals unit.KartyUnitId into units
                from unit in units.DefaultIfEmpty()
                orderby unit == null ? int.MaxValue : unit.Sequence,
                    karty.UnitOrder ?? int.MaxValue,
                    karty.KartyId
                select new KartyMeaningItemResponse
                {
                    KartyId = karty.KartyId,
                    UnitSequence = unit == null ? null : unit.Sequence,
                    UnitOrder = karty.UnitOrder,
                    NounText = karty.NounText,
                    Article = karty.Article,
                    Meaning = _context.KartyTranslations
                        .Where(row => row.KartyId == karty.KartyId && row.Locale == locale)
                        .Select(row => row.Meaning)
                        .FirstOrDefault(),
                })
            .ToListAsync(cancellationToken);

        return new GetKartyMeaningsResponse
        {
            Locale = locale,
            MissingCount = items.Count(item => item.Meaning is null),
            Items = items,
        };
    }
}
