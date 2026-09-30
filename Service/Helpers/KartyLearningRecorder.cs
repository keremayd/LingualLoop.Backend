using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

namespace Service.Helpers;

public static class KartyLearningRecorder
{
    public static async Task<UserKartyLearning> RecordAsync(
        LingualLoopContext context,
        string userId,
        int kartyId,
        CancellationToken cancellationToken)
    {
        var learning = await context.UserKartyLearnings
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.KartyId == kartyId,
                cancellationToken);
        var now = DateTime.UtcNow;
        if (learning is null)
        {
            learning = new UserKartyLearning
            {
                UserId = userId,
                KartyId = kartyId,
                CorrectCount = 1,
                Box = 1,
                DueDate = KartyLeitner.NextDue(1, now),
                FirstLearnedDate = now,
                LastCorrectDate = now,
            };
            context.UserKartyLearnings.Add(learning);
            return learning;
        }

        learning.CorrectCount += 1;
        learning.LastCorrectDate = now;

        // **Kutu yalnız vadesi dolduysa ilerler.** Aynı oturumda ikinci kez
        // doğru bilmek kutuyu ilerletmez; aralıklı tekrarın tamamı bu şarta
        // dayanıyor. Şart kalksaydı kutu, aralığı olmayan bir sayaca — yani
        // düzeltmeye çalıştığımız eski `CorrectCount`'a — geri dönerdi.
        if (KartyLeitner.IsDue(learning, now))
        {
            learning.Box = KartyLeitner.Advance(learning.Box);
            learning.DueDate = KartyLeitner.NextDue(learning.Box, now);
        }

        return learning;
    }
}
