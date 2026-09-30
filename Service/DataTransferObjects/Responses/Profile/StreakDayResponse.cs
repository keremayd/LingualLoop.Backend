namespace Service.DataTransferObjects.Responses.Profile;

/// <summary>Seri şeridindeki tek bir gün.</summary>
public class StreakDayResponse
{
    public DateOnly Date { get; set; }

    /// <summary>O gün uygulamaya girildi mi.</summary>
    public bool Active { get; set; }

    /// <summary>O gün bir koruma ile kurtarıldı mı (girilmedi ama seri kırılmadı).</summary>
    public bool Frozen { get; set; }
}
