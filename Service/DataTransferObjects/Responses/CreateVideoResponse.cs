namespace Service.DataTransferObjects.Responses;

public class CreateVideoResponse
{
    public string VideoUrl { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public string VideoDescription { get; set; } = string.Empty;
}