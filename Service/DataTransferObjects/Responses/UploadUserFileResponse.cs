namespace Service.DataTransferObjects.Responses;

public class UploadUserFileResponse
{
    public string Id { get; set; }
    public string ProfilePhoto { get; set; }
    public string SignedUrl { get; set; }
}