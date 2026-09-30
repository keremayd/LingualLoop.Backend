namespace Service.DataTransferObjects.Responses;

public class UpdateLivesResponse
{
    public string UserId { get; set; }
    public int Lives { get; set; }
    public int MaxLives { get; set; }

    /// <summary>
    /// Bilet gerçekten düşüldü mü. Bakiye yetmediğinde false döner; giriş
    /// akışı bunu hataya çevirip oyunu engeller, yanlış cevap akışı ise
    /// yok sayar (oyun ortasında bilet bitince oturum bozulmasın).
    /// </summary>
    public bool Spent { get; set; }

    /// <summary>Sıradaki biletin yenileneceği an; tavandaysa null.</summary>
    public DateTime? NextTicketAt { get; set; }
}
