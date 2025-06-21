using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class UploadPhotoByIdResponse
{
    public User User { get; set; }
}