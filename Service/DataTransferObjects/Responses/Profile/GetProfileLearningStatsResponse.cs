namespace Service.DataTransferObjects.Responses.Profile;

public class GetProfileLearningStatsResponse
{
    public int LearnedWordCount { get; set; }

    /// <summary>Mevcut Karty zorluk bandında tekrar zamanı gelen kelimeler.</summary>
    public int DueWordCount { get; set; }

    public int LearnedArticleCount { get; set; }
    public int ArticleInProgressCount { get; set; }
    public int ReviewPendingCount { get; set; }
    public int ReviewMistakeCount { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int TotalArticleCorrectAnswers { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }

    /// <summary>Elde kalan seri koruma sayısı.</summary>
    public int FreezeCount { get; set; }

    /// <summary>
    /// Son yedi gün. Profildeki seri şeridi bunu çizer; ayrı bir uç açmak
    /// yerine buraya eklendi çünkü profil zaten her açılışta bu isteği atıyor.
    /// </summary>
    public List<StreakDayResponse> Week { get; set; } = [];
}
