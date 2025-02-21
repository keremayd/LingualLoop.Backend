using System.ComponentModel;

namespace Common.Enums;

public enum ErrorCode
{
    [Description("Hata oluştu!")]
    GenericError = 4000,
    
    [Description("Example Error In service!")]
    ExampleErrorInService = 4102,
    
    [Description("Users tablosunda kayıt bulunamadı. userid = {0}")]
    NoDataInUsers = 4103,

    [Description("Users tablosunda kayıt oluşturulamadı! usernickname = {0}")]
    NoDataCreatedInUsers = 4104,
    
    [Description("UserNickname boş olamaz!")]
    UserNicknameCannotBeNullOrEmpty = 4105,
    
    [Description("UserNickname zaten kayıtlı! userid = {0}")]
    TheUserNicknameWasRegistered = 4106,
    
    [Description("UserVideoHistory tablosunda kayıt oluşturulamadı! videoid = {0}, userid = {1}")]
    NoDataCreatedInUserVideoHistory = 4107,
    
    [Description("Videos tablosunda zaten kayıtlı! videourl = {0}, videotitle = {1}")]
    TheVideoWasRegistered = 4108,
    
    [Description("Videos tablosunda kayıt oluşturulamadı! videourl = {0}, videotitle = {1}")]
    NoDataCreatedInVideos = 4109,
    
    [Description("Kullanıcının puanına uygun soru Questions tablosunda bulunamadı! userscore = {0}")]
    NoDataFoundInQuestions = 4110,
    
    [Description("Authentication gerçekleştirilemedi! Username veya password hatalı.")]
    TheUserAuthenticatedFailed = 4111,
    
    [Description("Kullanıcının kaydı oluşturulamadı! username = {0}")]
    TheUserNotCreatedInDatabase = 4112,
    
    [Description("Geçersiz veya süresi dolmuş refresh token. refreshtoken = {0}")]
    InvalidOrExpiredRefreshToken = 4113,
    
    [Description("UserScores tablosunda kayıt bulunamadı. userid = {0}")]
    NoDataInUserScores = 4114,
    
    [Description("Kullanıcı UserLives tablosunda bulunamadı! userid = {0}")]
    NoDataFoundInUserLives = 4115,
    
    [Description("Kullanıcının lives yeterli değil! lives = {0}")]
    TheUserHasNoLives = 4116,
    
    [Description("Kullanıcının puanına uygun soru Karty tablosunda bulunamadı! userscore = {0}")]
    NoDataFoundInKarty = 4117,
}