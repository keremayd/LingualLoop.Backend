using Postgres.Models;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Responses;

public class GetScoreWithLivesByIdResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserNickname { get; set; } = string.Empty;

    /// <summary>
    /// Eski zorluk termostatı. 4 Kas 2026'dan beri hiçbir şeyi belirlemiyor ve
    /// oynatılmıyor (içerik Karty Yolu üniteleriyle açılıyor); alan istemci
    /// modelleri kırılmasın diye duruyor. Kullanıcıya gösterilmez.
    /// </summary>
    public int Score { get; set; }

    /// <summary>İlerleme sayacı; yalnızca artar. Kullanıcıya gösterilen sayı.</summary>
    public int Experience { get; set; }

    public int Lives { get; set; }

    /// <summary>Bilet tavanı; premium ileride bunu kullanıcı bazında yükseltecek.</summary>
    public int MaxLives { get; set; }

    /// <summary>
    /// Sıradaki biletin geleceği an. Tavandayken null — beklenen bir şey yok.
    /// Bilet bitti ekranındaki geri sayım bunu kullanır.
    /// </summary>
    public DateTime? NextTicketAt { get; set; }

    public LeagueProgressResponse? League { get; set; }

    /// <summary>Ana ekranın üst şeridi için: günlük seri ve elde kalan koruma.</summary>
    public int Streak { get; set; }

    /// <summary>Geçmişte ulaşılan en uzun seri; ana ekran sahne seçimi için.</summary>
    public int LongestStreak { get; set; }

    public int FreezeCount { get; set; }

    /// <summary>
    /// Bugün gerçekten oynandı mı. Ana ekrandaki seri şeridinin ana sinyali;
    /// seri sayısı tek başına bugünün kurtarılıp kurtarılmadığını söylemiyor.
    ///
    /// Bu uçta duruyor çünkü oyundan dönüldüğünde tazelenen uç bu:
    /// `home_screen` oyun sonrası `scoreWithLivesById` çağırıyor. Şeridin
    /// oynadıktan hemen sonra güncellenmesi buna bağlı.
    /// </summary>
    public bool PlayedToday { get; set; }

    /// <summary>Son yedi gün; ana ekrandaki seri şeridi için.</summary>
    public List<StreakDayResponse> StreakWeek { get; set; } = new();
}
