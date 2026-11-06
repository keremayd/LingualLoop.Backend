namespace Service.DataTransferObjects.Responses.Karty;

/// <summary>
/// Karty Yolu: üniteler ve her ünitenin adımları. Bir adım, 8 yeni
/// kelimelik bir deste; her gün oynayan kullanıcıda bir güne denk gelir.
/// Görsel alanları S3 anahtarıdır, controller imzalı adrese çevirir.
/// </summary>
public class GetKartyPathResponse
{
    public List<KartyPathUnitResponse> Units { get; set; } = new();

    /// <summary>Günlük yeni kelime tavanı (bugünün halkasındaki parça sayısı).</summary>
    public int DailyNewWordLimit { get; set; }

    /// <summary>
    /// Yarının (İstanbul günü) başladığı an, UTC. Yarının penceresindeki geri
    /// sayım istemcide bundan hesaplanır; yerel saatle hesaplamak
    /// <c>nextTicketAt</c>'teki saat dilimi hatasını tekrar ederdi.
    /// </summary>
    public DateTime TomorrowStartsAtUtc { get; set; }

    /// <summary>
    /// Açılıp kutlaması henüz gösterilmemiş ünitenin sırası; yoksa null.
    /// İstemci patikaya dönünce kutlamayı gösterir ve
    /// <c>path/{userId}/unlock/acknowledge</c> ile kapatır.
    /// </summary>
    public int? PendingUnlockSequence { get; set; }
}

public class KartyPathUnitResponse
{
    public int Sequence { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary><c>done</c> · <c>current</c> · <c>open</c> · <c>locked</c>.</summary>
    public string State { get; set; } = string.Empty;

    public int WordCount { get; set; }

    /// <summary><c>box ≥ 2</c> olan kelime sayısı.</summary>
    public int ConsolidatedCount { get; set; }

    public double UnlockGoal { get; set; }
    public string? EmblemUrl { get; set; }
    public List<KartyPathStepResponse> Steps { get; set; } = new();
}

public class KartyPathStepResponse
{
    /// <summary><c>done</c> · <c>today</c> · <c>tomorrow</c> · <c>upcoming</c> · <c>locked</c>.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Geçmiş günün ve bugünün İstanbul tarihi: <c>2026-11-02</c>.</summary>
    public string? Day { get; set; }

    /// <summary>Bu destede tanışılan yeni kelime.</summary>
    public int NewCount { get; set; }

    /// <summary>Destenin büyüklüğü; bugün için günün hedefi.</summary>
    public int NewGoal { get; set; }

    /// <summary>Bugün ve yarın yapılacak tekrar sayısı.</summary>
    public int ReviewCount { get; set; }

    /// <summary>Bu destede tekrar vakti gelmiş kelime (geçmiş düğümün rozeti).</summary>
    public int DueCount { get; set; }

    public List<KartyPathWordResponse> Words { get; set; } = new();

    /// <summary>Yarının kelimelerinin görselleri; istemci siluet olarak çizer.</summary>
    public List<string> UpcomingImageUrls { get; set; } = new();
}

public class KartyPathWordResponse
{
    public int KartyId { get; set; }
    public string NounText { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string KartyUrl { get; set; } = string.Empty;
    public int Box { get; set; }
    public bool IsDue { get; set; }
}
