namespace Service.DataTransferObjects.Responses;

public class PremiumStatusResponse
{
    public bool IsPremium { get; set; }
    public DateTime? PremiumUntil { get; set; }
}
