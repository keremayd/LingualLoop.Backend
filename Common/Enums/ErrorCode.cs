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
}