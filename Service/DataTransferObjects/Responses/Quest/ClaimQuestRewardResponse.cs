namespace Service.DataTransferObjects.Responses;

public class ClaimQuestRewardResponse
{
    public string QuestKey { get; set; } = string.Empty;
    public bool Claimed { get; set; }

    /// <summary>Bakiyeye gerçekten eklenen bilet (tavana takılmışsa kesilmiş hâli).</summary>
    public int RewardTickets { get; set; }

    public int Lives { get; set; }
    public int MaxLives { get; set; }

    /// <summary>Tavan yüzünden verilemeyen bilet sayısı; 0 ise kesinti yok.</summary>
    public int CappedTickets { get; set; }

    /// <summary>
    /// Bakiye tavanda olduğu için ödül **hiç** verilemedi ve talep
    /// tüketilmedi: görev alınabilir kalır, kullanıcı yer açınca alır.
    ///
    /// Eskiden bu durumda talep kaydediliyor ama sıfır bilet veriliyordu;
    /// kullanıcı düğmeye basıp ödülü kalıcı olarak kaybediyordu.
    /// </summary>
    public bool BlockedByCap { get; set; }
}
