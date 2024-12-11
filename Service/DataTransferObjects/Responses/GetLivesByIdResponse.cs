namespace Service.DataTransferObjects.Responses;

public class GetLivesByIdResponse
{
    public string UserId { get; set; }
    public int Lives { get; set; }
    public int MaxLives { get; set; }
    public DateTime? LastLivesResetTime { get; set; }
}