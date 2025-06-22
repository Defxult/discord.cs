global using JSON = System.Collections.Generic.Dictionary<string, object?>;
namespace Discord.Utility;
using System.ComponentModel;

internal static class Enums
{
    /// <summary>
    /// Gets the value from the Description attribute.
    /// </summary>
    internal static string GetDescription(this Enum? value)
    {
        if (value == null)
            return string.Empty;
        return value.GetType()
                .GetField(value.ToString())?
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault() is not DescriptionAttribute attribute ? value.ToString() : attribute.Description;
    }
}