using Postgres.Abstractions;
using Postgres.Models;

namespace LingualLoop.Hangfire.Jobs;

public class UpdateLivesSyncJob
{
    private readonly ILingualLoopGenericRepository<UserLives> _userLivesRepository;

    public UpdateLivesSyncJob(ILingualLoopGenericRepository<UserLives> userLivesRepository)
    {
        _userLivesRepository = userLivesRepository;
    }

    public async Task Execute()
    {
        var userList = await _userLivesRepository.GetListAsync(u => u.LastLivesResetTime <= DateTime.UtcNow && u.Lives < u.MaxLives,
                user => new UserLives(){UserLivesId = user.UserLivesId, UserId = user.UserId, Lives = user.Lives, MaxLives = user.MaxLives, LastLivesResetTime = user.LastLivesResetTime});

        foreach (var user in userList)
        {
            user.Lives += 1;
            user.LastLivesResetTime = DateTime.UtcNow.AddMinutes(5);
            
            _userLivesRepository.Update(user);
            await _userLivesRepository.SaveChangesAsync(CancellationToken.None);
        }
    }
}