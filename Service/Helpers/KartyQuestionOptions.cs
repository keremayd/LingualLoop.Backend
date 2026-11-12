using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.Helpers;

/// <summary>
/// Görsel seçilen soruların seçenekleri. Çeldiriciler önce kullanıcının
/// tanıştığı kelimelerden ve ünite sırasında hedefe en yakın olanlardan
/// (aynı deste) gelir: tanımadığı bir görsel elenmesi kolay bir seçenektir.
/// </summary>
public static class KartyQuestionOptions
{
    private const int Distractors = 3;

    /// <summary>Çeldiricilerin rastgele seçildiği en yakın aday sayısı: bir deste.</summary>
    private const int NearbyPool = KartyDeckRules.Size - 1;

    public sealed record Candidate(int KartyId, string KartyUrl, int? UnitOrder, bool Seen);

    /// <summary>
    /// Görsel seçilen modda seçenekleri yazar ve geçerli modu döndürür;
    /// yeterli görsel yoksa soru artikele döner.
    /// </summary>
    public static async Task<KartyCardMode> FillAsync(
        LingualLoopContext context,
        string userId,
        GetKartyByScoreResponse question,
        KartyCardMode mode,
        CancellationToken cancellationToken)
    {
        if (mode is not (KartyCardMode.PickImage or KartyCardMode.Listen)) return mode;

        question.Options = await BuildAsync(context, userId, question, cancellationToken);
        return question.Options.Count > 1 ? mode : KartyCardMode.PickArticle;
    }

    private static async Task<List<KartyOptionResponse>> BuildAsync(
        LingualLoopContext context,
        string userId,
        GetKartyByScoreResponse question,
        CancellationToken cancellationToken)
    {
        var target = await context.Karty
            .AsNoTracking()
            .Where(k => k.KartyId == question.KartyId)
            .Select(k => new { k.UnitId, k.UnitOrder })
            .FirstAsync(cancellationToken);

        var seen = context.UserKartyLearnings
            .Where(l => l.UserId == userId)
            .Select(l => l.KartyId);
        var others = context.Karty
            .AsNoTracking()
            .Where(k => k.KartyId != question.KartyId &&
                        k.NounText != question.CorrectText &&
                        k.KartyUrl != "");

        var sameUnit = await others
            .Where(k => k.UnitId == target.UnitId)
            .Select(k => new Candidate(k.KartyId, k.KartyUrl, k.UnitOrder, seen.Contains(k.KartyId)))
            .ToListAsync(cancellationToken);
        var chosen = Choose(sameUnit, target.UnitOrder, Distractors);
        if (chosen.Count < Distractors)
        {
            var elsewhere = await others
                .Where(k => k.UnitId != target.UnitId)
                .Select(k => new Candidate(k.KartyId, k.KartyUrl, k.UnitOrder, seen.Contains(k.KartyId)))
                .ToListAsync(cancellationToken);
            chosen.AddRange(Choose(elsewhere, target.UnitOrder, Distractors - chosen.Count));
        }

        return chosen
            .Select(c => new KartyOptionResponse { KartyId = c.KartyId, KartyUrl = c.KartyUrl })
            .Append(new KartyOptionResponse { KartyId = question.KartyId, KartyUrl = question.KartyUrl })
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
    }

    /// <summary>Tanışılanlar önce, hedefe yakın olanlar önce; seçim o pencereden rastgele.</summary>
    public static List<Candidate> Choose(IEnumerable<Candidate> candidates, int? targetOrder, int count) =>
        candidates
            .OrderByDescending(c => c.Seen)
            .ThenBy(c => Math.Abs((c.UnitOrder ?? 0) - (targetOrder ?? 0)))
            .Take(Math.Max(NearbyPool, count))
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();
}
