using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.Helpers;

/// <summary>
/// Seri şeridini kurar: son yedi gün, her biri "girildi / korundu / boş".
///
/// "Korundu" günü ayrı bir durumdur çünkü kullanıcının o gün girmediği ama
/// serisinin kırılmadığı bilgisi anlamlıdır — şeritte boş bir gün görüp
/// "ama serim devam ediyor?" diye şaşırmasın.
///
/// Bu bilgi seri uzunluğundan **çıkarılmaz**, `user_daily_activity.frozen`
/// bayrağından okunur. Çıkarım denendi ve yanlış sonuç verdi: seri sayacı ile
/// aktivite satırları ayrı yazıldığı için aralarındaki her boşluk "korunmuş
/// gün" gibi görünüyordu (13 günlük seride 6 gün sahte kar tanesi).
/// </summary>
public static class StreakWeekBuilder
{
    public const int Days = 7;

    public static async Task<List<StreakDayResponse>> BuildAsync(
        LingualLoopContext context,
        string userId,
        DateOnly today,
        UserStreak? streak,
        CancellationToken cancellationToken)
    {
        var firstDay = today.AddDays(-(Days - 1));

        var rows = await context.UserDailyActivities
            .AsNoTracking()
            .Where(a => a.UserId == userId &&
                        a.ActivityDate >= firstDay &&
                        a.ActivityDate <= today)
            .Select(a => new { a.ActivityDate, a.Frozen, a.Played })
            .ToListAsync(cancellationToken);

        // "Girildi" değil **"oynandı"** aranıyor: seri gerçek etkinliğe
        // bağlandığı için şerit de aynı ölçüyü göstermeli. Aksi hâlde kullanıcı
        // şeritte dolu bir gün görüp serisinin neden artmadığını anlayamazdı.
        var activeSet = rows
            .Where(r => r.Played && !r.Frozen)
            .Select(r => r.ActivityDate)
            .ToHashSet();
        var frozenSet = rows.Where(r => r.Frozen).Select(r => r.ActivityDate).ToHashSet();

        var week = new List<StreakDayResponse>(Days);
        for (var offset = 0; offset < Days; offset++)
        {
            var date = firstDay.AddDays(offset);

            week.Add(new StreakDayResponse
            {
                Date = date,
                Active = activeSet.Contains(date),
                Frozen = frozenSet.Contains(date),
            });
        }

        return week;
    }
}
