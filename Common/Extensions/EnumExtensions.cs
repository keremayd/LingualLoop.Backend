using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Common.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this System.Enum enumValue, params object?[] args)
    {
        string enumText = enumValue.ToString();

        FieldInfo fi = enumValue.GetType().GetField(enumText)!;

        string description = fi.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>()
            .Select(x => x.Description)
            .FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(description))
        {
            return enumText;
        }

        try
        {
            return args.Length > 0 ? string.Format(CultureInfo.CurrentCulture, description, args) : description;
        }
        catch (FormatException)
        {
            return enumText;
        }
        catch (ArgumentNullException)
        {
            return enumText;
        }
    }
}