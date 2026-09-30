namespace Service.DataTransferObjects.Responses.ArticlePractice;

public class GetArticlePracticeTaskResponse
{
    public bool IsUnlocked { get; set; }
    public int LearnedWordCount { get; set; }
    public int RequiredWordCount { get; set; } = 2;
    public ArticlePracticeTaskResponse? Task { get; set; }
    public ArticlePracticeTaskResponse? NextTask { get; set; }
}

public class ArticlePracticeTaskResponse
{
    public int KartyId { get; set; }
    public string NounText { get; set; } = string.Empty;
    public string KartyUrl { get; set; } = string.Empty;
    public int ArticleAttemptCount { get; set; }
    public int ArticleCorrectCount { get; set; }
    public int LearningStack { get; set; }
    public int LearningGoal { get; set; } = 5;
}
