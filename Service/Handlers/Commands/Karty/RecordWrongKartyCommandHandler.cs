using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Commands.Karty;

public class RecordWrongKartyCommandHandler : IRequestHandler<RecordWrongKartyRequest, RecordWrongKartyResponse>
{
    private readonly LingualLoopContext _context;

    public RecordWrongKartyCommandHandler(ILingualLoopGenericRepository<UserKartyHistory> userKartyHistoryRepository)
    {
        _context = userKartyHistoryRepository.GetDbContext();
    }

    public async Task<RecordWrongKartyResponse> Handle(RecordWrongKartyRequest request, CancellationToken cancellationToken)
    {
        await UserKartyHistorySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var history = await _context.UserKartyHistories
            .FirstOrDefaultAsync(
                item => item.UserId == request.UserId && item.KartyId == request.KartyId,
                cancellationToken);

        if (history is null)
        {
            history = new UserKartyHistory
            {
                UserId = request.UserId,
                KartyId = request.KartyId,
                WrongCount = 1,
                LastWrongDate = DateTime.UtcNow,
            };

            _context.UserKartyHistories.Add(history);
        }
        else
        {
            history.WrongCount += 1;
            history.LastWrongDate = DateTime.UtcNow;
            history.ReviewedDate = null;
        }

        // Yanlış cevap **iki** sistemi birden tetikler ve ikisi farklı soru
        // sorar:
        //   · Rövanş havuzu (yukarısı) — "hemen düzelt"
        //   · Leitner kutusu (aşağısı) — "uzun vadede hatırlıyor musun"
        // Yalnız biri çalışsaydı ya hata anında düzeltilir ama aralık
        // korunur (kelime yanlış bilinmesine rağmen 30 gün sonra sorulur),
        // ya da aralık sıfırlanır ama hata o an düzeltilmeden geçerdi.
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var learning = await _context.UserKartyLearnings
            .FirstOrDefaultAsync(
                item => item.UserId == request.UserId && item.KartyId == request.KartyId,
                cancellationToken);

        if (learning is not null && learning.Box > 0)
        {
            learning.Box = KartyLeitner.Reset();
            learning.DueDate = KartyLeitner.NextDue(learning.Box, DateTime.UtcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new RecordWrongKartyResponse
        {
            UserId = history.UserId,
            KartyId = history.KartyId,
            WrongCount = history.WrongCount,
            LastWrongDate = history.LastWrongDate,
        };
    }
}
