using System.ComponentModel;

namespace Common.Enums;

public enum ErrorCode
{
    [Description("Hata oluştu!")]
    GenericError = 4000,
    
    [Description("Example Error In service!")]
    ExampleErrorInService = 4102,
    
    [Description("User tablosunda kayıt bulunamadı. userid = {0}")]
    NoDataInUser = 4103,

    [Description("User tablosunda kayıt oluşturulamadı! usernickname = {0}")]
    NoDataCreatedInUser = 4104,
    
    [Description("UserNickname boş olamaz!")]
    UserNicknameCannotBeNullOrEmpty = 4105,
    
    [Description("UserNickname zaten kayıtlı! userid = {0}")]
    TheUserNicknameWasRegistered = 4106,
}