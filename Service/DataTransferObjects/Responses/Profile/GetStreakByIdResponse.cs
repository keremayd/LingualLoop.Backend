namespace Service.DataTransferObjects.Responses.Profile;

/// <summary>
/// Ana ekranın üst şeridi için hafif seri okuması.
///
/// `GetProfileLearningStats` de bu iki alanı döndürüyor ama yanında yedi ayrı
/// sayım sorgusu çalıştırıyor; ana ekranın o yüke ihtiyacı yok.
/// </summary>
public class GetStreakByIdResponse
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int FreezeCount { get; set; }

    /// <summary>
    /// Bugün gerçekten **oynandı** mı. Ana ekrandaki seri şeridinin ana
    /// sinyali: seri sayısı tek başına bugünün kurtarılıp kurtarılmadığını
    /// söylemiyor.
    /// </summary>
    public bool PlayedToday { get; set; }

    /// <summary>
    /// Son yedi gün. Profildeki şeritle aynı yapı ve aynı kaynak
    /// (`StreakWeekBuilder`); iki ayrı hesap olsaydı zamanla ayrışırlardı.
    ///
    /// Uç hâlâ hafif kalıyor: profil istatistiklerinin yedi ayrı sayımına
    /// karşılık burada tek bir aralık sorgusu var.
    /// </summary>
    public List<StreakDayResponse> Week { get; set; } = new();
}
