using MediatR;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Requests.Profile;

public class ResolveStreakFreezeRequest : IRequest<RecordDailyActivityResponse>
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// true: koruma harcanır, seri devam eder.
    /// false: kullanıcı korumayı saklamayı seçti, seri sıfırlanır.
    /// </summary>
    public bool UseFreeze { get; set; }
}
