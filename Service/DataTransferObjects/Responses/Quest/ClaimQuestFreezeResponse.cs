namespace Service.DataTransferObjects.Responses;

public class ClaimQuestFreezeResponse
{
    public bool Claimed { get; set; }
    public int FreezeCount { get; set; }
    public int FreezeCap { get; set; }

    /// <summary>
    /// Koruma tavanda olduğu için verilemedi ve talep **tüketilmedi**:
    /// kullanıcı korumayı harcayınca aynı gün yine toplayabilir (bilet
    /// ödülündeki tavan kuralıyla aynı).
    /// </summary>
    public bool BlockedByCap { get; set; }
}
