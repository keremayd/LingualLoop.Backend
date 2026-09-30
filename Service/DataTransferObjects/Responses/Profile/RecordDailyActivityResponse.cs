namespace Service.DataTransferObjects.Responses.Profile;

public class RecordDailyActivityResponse
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly LastActiveDate { get; set; }

    /// <summary>Elde kalan seri koruma sayısı.</summary>
    public int FreezeCount { get; set; }

    /// <summary>
    /// Seri kırılmak üzere ve kullanıcının bunu önleyecek koruması var.
    /// Koruma **otomatik harcanmaz**: az günlük bir seri için kıymetli bir
    /// korumayı yakmak istemeyebilir, kararı kullanıcı verir.
    /// Karar verilene kadar seri ne artar ne sıfırlanır; her açılışta
    /// yeniden sorulur.
    /// </summary>
    public bool StreakAtRisk { get; set; }

    /// <summary>Risk durumunda kaç gün kaçırıldığı — o kadar koruma gerekir.</summary>
    public int MissedDays { get; set; }

    /// <summary>Son yedi gün; seri şeridi bunu çizer.</summary>
    public List<StreakDayResponse> Week { get; set; } = [];

    public bool CheckedInToday { get; set; }
}
