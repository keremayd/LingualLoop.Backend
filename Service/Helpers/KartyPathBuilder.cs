using Service.DataTransferObjects.Responses.Karty;

namespace Service.Helpers;

/// <summary>
/// Karty Yolu'nun adımlarını kurar. Saf: veritabanı bilmez, saati parametre
/// alır; bu yüzden elle sınanabilir.
///
/// ## Adım ne demek
///
/// Bir adım bir deste (<see cref="KartyDeckRules"/>): ünitenin kelimeleri
/// ünite sırasıyla 8'erli bölünür. Adımlar günlere göre değil, içeriğe göre
/// kurulur — patika takvim değil, yürünen bir yol.
///
/// <code>
/// açık ünite
///   ├─ bitmiş desteler      → done      (her kelimesi en az bir kez doğru)
///   ├─ ilk bitmemiş deste   → current   (DEVAM balonu, kelime halkası)
///   └─ sonraki desteler     → upcoming  (kelimeleri silüet)
/// kilitli ünite              → locked
/// </code>
///
/// Açık ünitelerde bitmemiş deste kalmadıysa (ünite kapısı kapalı ya da
/// içerik bitti) son açık ünitenin sonuna yalnız tekrardan oluşan bir
/// <c>current</c> adım eklenir: DEVAM her zaman bir yere bağlı kalır.
/// Vadesi gelmiş tekrarlar bütün ünitelerden sayılır; <c>current</c> adıma
/// oyunun soracağı kadarı yazılır (<see cref="KartyDeckRules.ReviewQuota"/>),
/// tekrarlar yeni kelimelerle karışık gelir, kalanı sonraki oyunlara kalır.
/// </summary>
public static class KartyPathBuilder
{
    public static GetKartyPathResponse Build(
        IReadOnlyList<PathUnit> units,
        IReadOnlyList<PathCard> cards,
        IReadOnlyList<PathLearning> learnings,
        DateTime utcNow,
        int recordedUnlockSequence = 0)
    {
        var learningById = learnings.ToDictionary(item => item.KartyId);
        var cardsByUnit = units.ToDictionary(
            unit => unit.UnitId,
            unit => cards
                .Where(card => card.UnitId == unit.UnitId)
                .OrderBy(card => card.UnitOrder ?? int.MaxValue)
                .ThenBy(card => card.KartyId)
                .ToList());

        var progress = units
            .Select(unit =>
            {
                var unitCards = cardsByUnit[unit.UnitId];
                var seen = unitCards.Where(card => learningById.ContainsKey(card.KartyId)).ToList();
                return new UnitProgress(
                    unitCards.Count,
                    seen.Count,
                    seen.Count(card => KartyUnitRules.IsConsolidated(learningById[card.KartyId].Box)));
            })
            .ToList();
        // Kelime seçiciyle aynı kural ve aynı kayıt: "hangi ünite açık" iki
        // yerde farklı hesaplanmasın.
        var unlocked = KartyUnitRules.UnlockStates(
            progress,
            KartyUnitRules.RecordedIndex(
                units.Select(unit => unit.Sequence).ToList(),
                recordedUnlockSequence));
        var reviews = learnings.Count(item =>
            KartyLeitner.IsDueForReview(item.Box, item.DueDate, utcNow));

        var response = new GetKartyPathResponse { DeckSize = KartyDeckRules.Size };
        KartyPathStepResponse? current = null;

        for (var i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            var unitCards = cardsByUnit[unit.UnitId];
            var emblemKey = unitCards.FirstOrDefault(card => card.KartyId == unit.EmblemKartyId)?.KartyUrl
                            ?? unitCards.FirstOrDefault()?.KartyUrl;
            var unitResponse = new KartyPathUnitResponse
            {
                Sequence = unit.Sequence,
                Title = unit.Title,
                State = unlocked[i] ? "open" : "locked",
                WordCount = progress[i].WordCount,
                ConsolidatedCount = progress[i].ConsolidatedCount,
                UnlockGoal = KartyUnitRules.UnlockRatio,
                EmblemUrl = emblemKey,
            };
            response.Units.Add(unitResponse);

            var decks = KartyDeckRules.Split(unitCards);
            for (var d = 0; d < decks.Count; d++)
            {
                var deck = decks[d];
                if (!unlocked[i])
                {
                    // Kilitli ünite: kelimeleri gösterilmez, yalnız kaç deste olduğu.
                    unitResponse.Steps.Add(new KartyPathStepResponse
                    {
                        State = "locked",
                        Deck = d + 1,
                        NewGoal = deck.Count,
                    });
                    continue;
                }

                var seen = deck.Where(card => learningById.ContainsKey(card.KartyId)).ToList();
                var tested = seen.Count(card => learningById[card.KartyId].Box >= 1);
                var state = KartyDeckRules.IsComplete(deck.Count, tested)
                    ? "done"
                    : current is null ? "current" : "upcoming";
                var step = new KartyPathStepResponse
                {
                    State = state,
                    Deck = d + 1,
                    NewCount = seen.Count,
                    NewGoal = deck.Count,
                    DueCount = seen.Count(card => KartyLeitner.IsDueForReview(
                        learningById[card.KartyId].Box,
                        learningById[card.KartyId].DueDate,
                        utcNow)),
                    Words = seen
                        .Select(card => ToWord(card, learningById[card.KartyId], utcNow))
                        .ToList(),
                    UpcomingImageUrls = deck
                        .Where(card => !learningById.ContainsKey(card.KartyId))
                        .Select(card => card.KartyUrl)
                        .ToList(),
                };
                if (state == "current") current = step;
                unitResponse.Steps.Add(step);
            }
        }

        if (current is null)
        {
            // Açık bütün desteler bitti: sıradaki adım yalnız tekrar.
            var host = response.Units.LastOrDefault(unit => unit.State != "locked");
            if (host is not null)
            {
                current = new KartyPathStepResponse { State = "current" };
                host.Steps.Add(current);
            }
        }
        if (current is not null)
        {
            current.ReviewCount = Math.Min(
                reviews,
                KartyDeckRules.ReviewQuota(deckGame: current.Deck is not null));
        }

        AssignUnitStates(response);
        return response;
    }

    /// <summary>
    /// <c>current</c> adımı taşıyan ünite güncel; öncesindeki açık üniteler
    /// tamamlanmış, sonrasındakiler açık ama başlanmamış.
    /// </summary>
    private static void AssignUnitStates(GetKartyPathResponse response)
    {
        var currentIndex = response.Units.FindIndex(
            unit => unit.Steps.Any(step => step.State == "current"));
        for (var i = 0; i < response.Units.Count; i++)
        {
            var unit = response.Units[i];
            if (unit.State == "locked") continue;
            unit.State = i == currentIndex ? "current" : i < currentIndex ? "done" : "open";
        }
    }

    private static KartyPathWordResponse ToWord(PathCard card, PathLearning learning, DateTime utcNow) =>
        new()
        {
            KartyId = card.KartyId,
            NounText = card.NounText,
            Article = card.Article,
            KartyUrl = card.KartyUrl,
            Box = learning.Box,
            IsDue = KartyLeitner.IsDueForReview(learning.Box, learning.DueDate, utcNow),
        };
}

public sealed record PathUnit(int UnitId, int Sequence, string Title, int? EmblemKartyId);

public sealed record PathCard(
    int KartyId,
    int UnitId,
    int? UnitOrder,
    string NounText,
    string Article,
    string KartyUrl);

public sealed record PathLearning(
    int KartyId,
    int Box,
    DateTime? DueDate,
    DateTime? IntroducedDate,
    DateTime FirstLearnedDate);
