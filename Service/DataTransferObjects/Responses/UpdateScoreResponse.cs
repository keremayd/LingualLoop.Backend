namespace Service.DataTransferObjects.Responses;

public class UpdateScoreResponse
{
    public string UserId { get; set; } = string.Empty;
    public int? Score { get; set; }
    public int? Lives { get; set; }
}