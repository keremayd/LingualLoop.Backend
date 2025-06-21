using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class UploadPhotoByIdRequest : IRequest<UploadPhotoByIdResponse>
{
    public string Id { get; set; }
    public string PhotoUrl { get; set; }
}