namespace Service.DataTransferObjects.Responses;

public class RecordWatchVideoByUserResponse
{
    public int VideoId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime WatchedDate { get; set; }
}