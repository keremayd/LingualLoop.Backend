using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;
using KartyModel = Postgres.Models.Karty;

namespace Service.Handlers.Commands.Karty;

/// <summary>
/// Bir dildeki anlamları kartın Almanca adına göre toplu yazar
/// (<see cref="UpsertKartyMeaningsRequest"/>). Hepsi tek işlemde: geçersiz
/// bir değer varsa hiçbiri yazılmaz.
/// </summary>
public class UpsertKartyMeaningsCommandHandler
    : IRequestHandler<UpsertKartyMeaningsRequest, UpsertKartyMeaningsResponse>
{
    private readonly StelindaContext _context;

    public UpsertKartyMeaningsCommandHandler(IStelindaGenericRepository<KartyModel> kartyRepository)
    {
        _context = kartyRepository.GetDbContext();
    }

    public async Task<UpsertKartyMeaningsResponse> Handle(
        UpsertKartyMeaningsRequest request,
        CancellationToken cancellationToken)
    {
        await KartyTranslationSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var locale = KartyTranslationWriter.RequireLocale(request.Locale);
        var response = new UpsertKartyMeaningsResponse { Locale = locale };

        // Önce doğrula: uzun bir değer varsa hiçbir şey yazılmasın.
        var meanings = new List<(string Noun, string Meaning)>();
        foreach (var (noun, raw) in request.Meanings)
        {
            var meaning = (raw ?? string.Empty).Trim();
            if (meaning.Length > KartyTranslationWriter.MaxLength)
            {
                var problem = $"'{noun}' anlamı {KartyTranslationWriter.MaxLength} karakteri aşıyor.";
                throw new StelindaException(
                    ErrorCode.KartyPayloadInvalid.CreateMessage(problem),
                    ErrorCode.KartyPayloadInvalid.GetDescription(problem),
                    HttpStatusCode.BadRequest);
            }

            meanings.Add((noun.Trim(), meaning));
        }

        var cards = await _context.Karty
            .AsNoTracking()
            .Select(karty => new { karty.KartyId, karty.NounText })
            .ToListAsync(cancellationToken);
        var idsByNoun = cards
            .GroupBy(card => card.NounText.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(card => card.KartyId).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var rows = await _context.KartyTranslations
            .Where(row => row.Locale == locale)
            .ToDictionaryAsync(row => row.KartyId, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var (noun, meaning) in meanings)
        {
            if (!idsByNoun.TryGetValue(noun, out var ids))
            {
                response.UnknownNouns.Add(noun);
                continue;
            }

            if (ids.Count > 1)
            {
                response.AmbiguousNouns.Add(noun);
                continue;
            }

            var kartyId = ids[0];
            rows.TryGetValue(kartyId, out var row);
            if (meaning.Length == 0)
            {
                if (row is null) continue;
                _context.KartyTranslations.Remove(row);
                rows.Remove(kartyId);
                response.Removed++;
            }
            else if (row is null)
            {
                row = new KartyTranslation
                {
                    KartyId = kartyId,
                    Locale = locale,
                    Meaning = meaning,
                    UpdatedAt = now,
                };
                _context.KartyTranslations.Add(row);
                rows[kartyId] = row;
                response.Saved++;
            }
            else if (row.Meaning != meaning)
            {
                row.Meaning = meaning;
                row.UpdatedAt = now;
                response.Saved++;
            }
            else
            {
                response.Unchanged++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        response.TotalCards = cards.Count;
        response.MissingCount = cards.Count(card => !rows.ContainsKey(card.KartyId));
        return response;
    }
}
