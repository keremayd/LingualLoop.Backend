using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Commands.Karty;

public class RecordLearnedKartyCommandHandler
    : IRequestHandler<RecordLearnedKartyRequest, RecordLearnedKartyResponse>
{
    private readonly LingualLoopContext _context;

    public RecordLearnedKartyCommandHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<RecordLearnedKartyResponse> Handle(
        RecordLearnedKartyRequest request,
        CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var learning = await KartyLearningRecorder.RecordAsync(
            _context,
            request.UserId,
            request.KartyId,
            cancellationToken);

        // Yeni kelime öğrenmek de oynamaktır.
        await StreakActivityRecorder.MarkPlayedAsync(
            _context, request.UserId, DateTime.UtcNow, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        // Yalnız **öğrenilen** kelimeler sayılır; tanışma adımı da satır
        // oluşturuyor ama `CorrectCount = 0` ile.
        var learnedWordCount = await _context.UserKartyLearnings
            .CountAsync(
                item => item.UserId == request.UserId && item.CorrectCount > 0,
                cancellationToken);

        return new RecordLearnedKartyResponse
        {
            KartyId = learning.KartyId,
            CorrectCount = learning.CorrectCount,
            LearnedWordCount = learnedWordCount,
        };
    }
}
