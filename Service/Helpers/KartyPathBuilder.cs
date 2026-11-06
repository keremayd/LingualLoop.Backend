using Service.DataTransferObjects.Responses.Karty;

namespace Service.Helpers;

/// <summary>
/// Karty Yolu'nun adımlarını kurar. Saf: veritabanı bilmez, saati parametre
/// alır; bu yüzden gün sınırında ve ünite geçişinde elle sınanabilir.
///
/// ## Adım ne demek
///
/// Bir adım 8 yeni kelimelik bir deste (<see cref="KartyLeitner.DailyNewWordLimit"/>).
/// Geçmiş adımlar kelimelerin **tanışıldığı güne** göre gruplanır — kullanıcı
/// "geçmiş günlerdeki adımlarımı" görmek istiyor, deste sınırlarını değil.
/// Gelecek adımlar ise sıradaki kelimelerin plana dökülmüş hâli:
///
/// <code>
/// görülmemiş kelimeler (ünite sırasıyla)
///   ├─ bugünün kalan kotası     → bugünün düğümü (halkanın boş parçaları)
///   ├─ sonraki 8                → yarın
///   └─ kalanlar 8'er            → sonrası
/// </code>
///
/// Bugünün kotası yalnız **güncel** ünitede kullanılır; ünite bugün biterse
/// sıradaki ünite yarından başlar. Kilitli ünite plana girmez: kelime seçici
/// (<see cref="KartyScheduler"/>) oradan kelime vermiyor.
///
/// Yeni kelime yoksa (ünite kapısı kapalı ya da içerik bitti) bugünün ve
/// yarının düğümü yine çizilir, yalnız tekrarlarla: DEVAM her zaman bir yere
/// bağlı kalır.
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
        var limit = KartyLeitner.DailyNewWordLimit;
        var (todayStartUtc, todayEndUtc) = IstanbulDay.UtcRangeOf(utcNow);
        var tomorrowEndUtc = todayEndUtc.AddDays(1);
        var today = IstanbulDay.DateOf(utcNow);

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

        var introducedToday = learnings.Count(item =>
            item.IntroducedDate >= todayStartUtc && item.IntroducedDate < todayEndUtc);
        var remainingToday = Math.Max(0, limit - introducedToday);
        var reviewsToday = learnings.Count(item =>
            KartyLeitner.IsDueForReview(item.Box, item.DueDate, utcNow));
        var reviewsTomorrow = learnings.Count(item =>
            item.Box > 0 && item.DueDate > utcNow && item.DueDate < tomorrowEndUtc);

        var plan = PlanUnseen(units, cardsByUnit, learningById, progress, unlocked, remainingToday, limit);

        var response = new GetKartyPathResponse
        {
            DailyNewWordLimit = limit,
            TomorrowStartsAtUtc = todayEndUtc,
        };

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

            if (!unlocked[i])
            {
                // Kilitli ünite: içeriği belli değil, yalnız kaç deste olduğu.
                for (var left = unitCards.Count; left > 0; left -= limit)
                {
                    unitResponse.Steps.Add(new KartyPathStepResponse
                    {
                        State = "locked",
                        NewGoal = Math.Min(limit, left),
                    });
                }
                continue;
            }

            // Geçmiş günler (ve bugün tanışılanlar), tanışma gününe göre.
            var byDay = unitCards
                .Where(card => learningById.ContainsKey(card.KartyId))
                .GroupBy(card => DayOf(learningById[card.KartyId]))
                .OrderBy(group => group.Key);
            foreach (var group in byDay)
            {
                var words = group
                    .OrderBy(card => learningById[card.KartyId].IntroducedDate
                                     ?? learningById[card.KartyId].FirstLearnedDate)
                    .Select(card => ToWord(card, learningById[card.KartyId], utcNow))
                    .ToList();
                unitResponse.Steps.Add(new KartyPathStepResponse
                {
                    State = group.Key == today ? "today" : "done",
                    Day = group.Key.ToString("yyyy-MM-dd"),
                    NewCount = words.Count,
                    NewGoal = words.Count,
                    DueCount = words.Count(word => word.IsDue),
                    Words = words,
                });
            }

            // Plana dökülmüş görülmemiş kelimeler: bugün, yarın, sonrası.
            var planned = unitCards
                .Where(card => plan.ContainsKey(card.KartyId))
                .GroupBy(card => plan[card.KartyId])
                .OrderBy(group => group.Key);
            foreach (var group in planned)
            {
                var count = group.Count();
                if (group.Key == 0)
                {
                    var todayStep = unitResponse.Steps.LastOrDefault(step => step.State == "today");
                    if (todayStep is null)
                    {
                        unitResponse.Steps.Add(new KartyPathStepResponse
                        {
                            State = "today",
                            Day = today.ToString("yyyy-MM-dd"),
                            NewGoal = count,
                        });
                    }
                    else
                    {
                        todayStep.NewGoal += count;
                    }
                }
                else
                {
                    unitResponse.Steps.Add(new KartyPathStepResponse
                    {
                        State = group.Key == 1 ? "tomorrow" : "upcoming",
                        NewGoal = count,
                        UpcomingImageUrls = group.Key == 1
                            ? group.Select(card => card.KartyUrl).ToList()
                            : new List<string>(),
                    });
                }
            }
        }

        PlaceTodayAndTomorrow(response, reviewsToday, reviewsTomorrow, today);
        AssignUnitStates(response);
        return response;
    }

    /// <summary>Görülmemiş kelimeye gün atar: 0 = bugün, 1 = yarın, 2+ = sonrası.</summary>
    private static Dictionary<int, int> PlanUnseen(
        IReadOnlyList<PathUnit> units,
        IReadOnlyDictionary<int, List<PathCard>> cardsByUnit,
        IReadOnlyDictionary<int, PathLearning> learningById,
        IReadOnlyList<UnitProgress> progress,
        IReadOnlyList<bool> unlocked,
        int remainingToday,
        int limit)
    {
        var plan = new Dictionary<int, int>();
        var current = -1;
        for (var i = 0; i < units.Count; i++)
        {
            if (progress[i].SeenCount < progress[i].WordCount)
            {
                current = i;
                break;
            }
        }
        if (current < 0) return plan;

        var slot = 0;
        for (var i = current; i < units.Count && unlocked[i]; i++)
        {
            foreach (var card in cardsByUnit[units[i].UnitId])
            {
                if (learningById.ContainsKey(card.KartyId)) continue;
                plan[card.KartyId] = slot < remainingToday ? 0 : 1 + (slot - remainingToday) / limit;
                slot++;
            }
            // Bugünün kotası güncel üniteyle sınırlı.
            if (i == current) slot = Math.Max(slot, remainingToday);
        }

        return plan;
    }

    /// <summary>
    /// Bugünün ve yarının düğümü her zaman var: yeni kelime yoksa yalnız
    /// tekrarla, açık son ünitenin sonunda (kapısından önce).
    /// </summary>
    private static void PlaceTodayAndTomorrow(
        GetKartyPathResponse response,
        int reviewsToday,
        int reviewsTomorrow,
        DateOnly today)
    {
        var steps = response.Units.SelectMany(unit => unit.Steps).ToList();
        var host = response.Units.LastOrDefault(unit => unit.State != "locked");
        if (host is null) return;

        // Ünite bugün bitip sıradakine bugün başlandıysa iki "bugün" grubu
        // oluşur; bugünün düğümü yenisidir, öncekinin günü tamamlanmış sayılır.
        var todaySteps = steps.Where(step => step.State == "today").ToList();
        foreach (var earlier in todaySteps.SkipLast(1)) earlier.State = "done";

        var todayStep = todaySteps.LastOrDefault();
        if (todayStep is null)
        {
            todayStep = new KartyPathStepResponse
            {
                State = "today",
                Day = today.ToString("yyyy-MM-dd"),
            };
            host.Steps.Add(todayStep);
        }
        todayStep.ReviewCount = reviewsToday;

        var tomorrowStep = steps.FirstOrDefault(step => step.State == "tomorrow");
        if (tomorrowStep is null)
        {
            tomorrowStep = new KartyPathStepResponse { State = "tomorrow" };
            var todayHost = response.Units.First(unit => unit.Steps.Contains(todayStep));
            todayHost.Steps.Insert(todayHost.Steps.IndexOf(todayStep) + 1, tomorrowStep);
        }
        tomorrowStep.ReviewCount = reviewsTomorrow;
    }

    /// <summary>
    /// Bugünün düğümünü taşıyan ünite güncel; öncesindeki açık üniteler
    /// tamamlanmış, sonrasındakiler açık ama başlanmamış.
    /// </summary>
    private static void AssignUnitStates(GetKartyPathResponse response)
    {
        var currentIndex = response.Units.FindIndex(
            unit => unit.Steps.Any(step => step.State == "today"));
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

    /// <summary>
    /// Kelimenin tanışıldığı İstanbul günü. Eski satırlarda tanışma tarihi
    /// yok (satır bir dönem yalnız doğru cevapta açılıyordu); o zaman ilk
    /// doğru cevabın günü.
    /// </summary>
    private static DateOnly DayOf(PathLearning learning) =>
        IstanbulDay.DateOf(learning.IntroducedDate ?? learning.FirstLearnedDate);
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
