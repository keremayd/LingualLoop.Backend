using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_karty_learning")]
public class UserKartyLearning
{
    [Key]
    [Column("user_karty_learning_id")]
    public int UserKartyLearningId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("karty_id")]
    public int KartyId { get; set; }

    [Column("correct_count")]
    public int CorrectCount { get; set; }

    [Column("article_attempt_count")]
    public int ArticleAttemptCount { get; set; }

    [Column("article_correct_count")]
    public int ArticleCorrectCount { get; set; }

    [Column("consecutive_serve_count")]
    public int ConsecutiveServeCount { get; set; }

    /// <summary>
    /// Leitner kutusu. 0 = tanıştırıldı ama henüz doğru bilinmedi,
    /// 1–5 = her doğru cevapta bir kademe. Bkz. <c>KartyLeitner</c>.
    /// </summary>
    [Column("box")]
    public int Box { get; set; }

    /// <summary>
    /// Bir sonraki tekrarın **en erken** zamanı. Bu andan önce verilen doğru
    /// cevaplar kutuyu ilerletmez.
    /// </summary>
    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Kullanıcının bu kelimeyle **tanıştırıldığı** an. Satırın kendisi
    /// yetmiyor: satır bir dönem yalnız doğru cevapta oluşturuluyordu, yani
    /// varlığı "gördü" değil "bir kez bildi" anlamına geliyordu.
    /// </summary>
    [Column("introduced_date")]
    public DateTime? IntroducedDate { get; set; }

    [Column("first_learned_date")]
    public DateTime FirstLearnedDate { get; set; }

    [Column("last_correct_date")]
    public DateTime LastCorrectDate { get; set; }

    [Column("last_article_served_date")]
    public DateTime? LastArticleServedDate { get; set; }

    [Column("last_article_answered_date")]
    public DateTime? LastArticleAnsweredDate { get; set; }

    public User? User { get; set; }
    public Karty? Karty { get; set; }
}
