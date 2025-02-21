namespace Service.DataTransferObjects.Responses.Karty;

public class GetKartyByScoreResponse
{
    public int KartyId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string KartyUrl { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
}