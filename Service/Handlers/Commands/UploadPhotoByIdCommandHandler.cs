using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

public class UploadPhotoByIdCommandHandler : IRequestHandler<UploadPhotoByIdRequest, UploadPhotoByIdResponse>
{
    private readonly ILingualLoopGenericRepository<User> _userRepository;
    private readonly LingualLoopContext _context;
    
    public UploadPhotoByIdCommandHandler(ILingualLoopGenericRepository<User> userRepository)
    {
        _userRepository = userRepository;
        _context = userRepository.GetDbContext();
    }
    
    public async Task<UploadPhotoByIdResponse> Handle(UploadPhotoByIdRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken: cancellationToken);
        
        user!.ProfilePhoto = request.PhotoUrl;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);

        return new UploadPhotoByIdResponse()
        {
            User = user
        };
    }
}