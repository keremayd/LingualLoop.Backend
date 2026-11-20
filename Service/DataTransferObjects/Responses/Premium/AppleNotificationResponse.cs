namespace Service.DataTransferObjects.Responses;

public class AppleNotificationResponse
{
    /// İmza geçerli değilse false; Apple'a 400 döner.
    public bool Valid { get; set; }

    /// Bildirim bir aboneliği güncelledi mi (TEST bildirimi ya da bilinmeyen
    /// abonelik güncellemez ama geçerlidir).
    public bool Applied { get; set; }
}
