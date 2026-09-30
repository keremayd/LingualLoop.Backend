using Postgres.Models;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Responses;

public class GetScoreWithLivesByIdResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserNickname { get; set; } = string.Empty;

    /// <summary>Zorluk termostatı; iki yönlü. Kullanıcıya ham gösterilmez.</summary>
    public int Score { get; set; }

    /// <summary>İlerleme sayacı; yalnızca artar. Kullanıcıya gösterilen sayı.</summary>
    public int Experience { get; set; }

    /// <summary>Skordan türetilen zorluk seviyesi. Kullanıcı ham skoru değil bunu görür.</summary>
    public int Level { get; set; }

    /// <summary>Bulunulan seviyenin içindeki ilerleme (0 → LevelBandSize).</summary>
    public int LevelProgress { get; set; }

    /// <summary>Bir seviyenin kapsadığı skor aralığı; ilerleme çubuğu için.</summary>
    public int LevelBandSize { get; set; }

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
