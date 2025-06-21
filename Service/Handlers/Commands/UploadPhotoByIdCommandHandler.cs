using MediatR;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

public class UploadPhotoByIdCommandHandler : IRequestHandler<UploadPhotoByIdRequest, UploadPhotoByIdResponse>
{
    private readonly ILingualLoopGenericRepository<User> _userRepository;
    
    public UploadPhotoByIdCommandHandler(ILingualLoopGenericRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<UploadPhotoByIdResponse> Handle(UploadPhotoByIdRequest request, CancellationToken cancellationToken)
    {
        var user =  await _userRepository.FirstAsync(x => x.Id.Equals(request.Id), user => new User() { Id = user.Id, ProfilePhoto = user.ProfilePhoto });
        
        user!.ProfilePhoto = request.PhotoUrl;
        
        _userRepository.Update(user);

        return new UploadPhotoByIdResponse()
        {
            User = user
        };
    }
}