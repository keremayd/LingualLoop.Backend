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
}