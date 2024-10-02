using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class GetQuestionByScoreResponse
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
    public int VideoId { get; set; }
    public Video? Video { get; set; }
    public ICollection<Answer>? Answers { get; set; }
}