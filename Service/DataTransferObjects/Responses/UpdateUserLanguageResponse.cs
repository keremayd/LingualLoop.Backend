namespace Service.DataTransferObjects.Responses;

public class UpdateUserLanguageResponse
{
    /// <summary>Kaydedilen dil, kanonik hâliyle ("SQ-xk" → "sq").</summary>
    public string NativeLanguage { get; set; } = string.Empty;
}
