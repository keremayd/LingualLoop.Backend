namespace Service.Helpers;

/// <summary>
/// Zorluk seviyesinin tek doğruluk kaynağı.
///
/// `user_scores.score` zorluk termostatıdır ve yanlış cevapta düşer. Ham
/// hâliyle gösterilirse her hata bir kayıp gibi okunur; oysa niyet ceza değil
/// kalibrasyondur. Bu yüzden kullanıcı ham skoru değil, ondan türetilen
/// **seviyeyi** görür: seviye seyrek değiştiği için tek bir yanlış görünür bir
/// kayba dönüşmez, ama üst üste yanlışlar seviyeyi gerçekten düşürür.
///
/// Bant genişliği tek sabittir; ileride kart zorluk aralıklarıyla
/// hizalanacaksa yalnızca burası değişir.
/// </summary>
public static class LevelRules
{
    /// <summary>Bir seviyenin kapsadığı skor aralığı.</summary>
    public const int BandSize = 50;

    /// <summary>Skor 0 iken seviye 1'dir; kullanıcı "seviye 0" görmemeli.</summary>
    public static int GetLevel(int score)
    {
        if (score < 0) score = 0;
        return score / BandSize + 1;
    }

    /// <summary>Bulunulan seviyenin içinde kaç puan ilerlenmiş.</summary>
    public static int GetProgressInLevel(int score)
    {
        if (score < 0) score = 0;
        return score % BandSize;
    }
}
