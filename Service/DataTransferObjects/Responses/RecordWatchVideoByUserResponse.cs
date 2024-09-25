namespace Service.DataTransferObjects.Responses;

public class RecordWatchVideoByUserResponse
{
    public int VideoId { get; set; }
    public int UserId { get; set; }
    public DateTime WatchedDate { get; set; }
}