namespace Service.DataTransferObjects.Responses.Video;

public class GetVideosByIdResponse
{
    public int UserVideoId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Postgres.Models.Video Video { get; set; } = new Postgres.Models.Video();
    public DateTime SavedDate { get; set; }
}