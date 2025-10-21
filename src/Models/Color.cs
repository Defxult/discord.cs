namespace Discord.Models;

/// <summary>
/// Represents a Discord color.
/// </summary>
public struct Color : IEquatable<Color>
{
    public static readonly Color Blurple = new(0x5865F2);
    public static readonly Color Fuchsia = new(0xEB459E);
    public static readonly Color Red = new(0xFC0303);
    public static readonly Color DarkRed = new(0x910101);
    public static readonly Color Orange = new(0xFF992B);
    public static readonly Color Yellow = new(0xFFDC2B);
    public static readonly Color Green = new(0x2BFF32);
    public static readonly Color DarkGreen = new(0x026105);
    public static readonly Color SkyBlue = new(0x36B1D6);
    public static readonly Color DarkBlue = new(0x1021E3);
    public static readonly Color Purple = new(0x8F44F2);
    public static readonly Color Pink = new(0xFCA7F0);
    public static readonly Color Black = new(0x000001);
    public static readonly Color White = new(0xFFFFFF);
    public static readonly Color Gray = new(0xA6A6A6);
    public static readonly Color DarkTheme = new(0x2F3136);
    public static readonly Color Teal = new(0x008080);

    /// <summary>
    /// The maximum a color value can be (lowest is 0).
    /// </summary>
    public const int Max = 0xFFFFFF;

    /// <summary>
    /// Raw value for the color.
    /// </summary>
    public int Value
    {
        get => _value;
        set => _value = value is < 0 or > Max ? 0 : value;
    }
    private int _value;

    /// <summary>
    /// Initializes a new color instance using a value.
    /// </summary>
    /// <param name="value">A color code.</param>
    public Color(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new color instance based on RGB values.
    /// </summary>
    /// <param name="r">Red component of the color.</param>
    /// <param name="g">Green component of the color.</param>
    /// <param name="b">Blue component of the color.</param>
    public Color(byte r, byte g, byte b)
    {
        Value = (r & 0x0ff) << 16 | (g & 0x0ff) << 8 | b & 0x0ff;
    }

    /// <summary>
    /// Initializes a new color instance based on RGB values.
    /// </summary>
    /// <param name="hex">A hex value.</param>
    public Color(string hex)
    { 
        var cleansed = string.Join(string.Empty, hex.Where(char.IsLetterOrDigit));
        Value = Convert.ToInt32(cleansed, 16);
    }
    
    public bool Equals(Color other) => Value == other.Value;
    public override bool Equals(object? other) => other is Color color && Equals(color);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !left.Equals(right);
    
    /// <summary>
    /// Generates a random color.
    /// </summary>
    /// <returns>A random color.</returns>
    public static Color Random() =>
        new(new Random().Next(1, Max + 1));
    
    /// <summary>
    /// Convert the <see cref="RoleColor"/> to its <see cref="Color"/> equivalent.
    /// </summary>
    /// <returns>A color.</returns>
    public static Color FromRoleColor(RoleColor color) => 
        new(color.Primary);

    /// <summary>
    /// Convert the value to its individual RGB components.
    /// </summary>
    /// <returns>Each RGB value.</returns>
    public (int r, int g, int b) ToRgb()
    {
        var r = Value >> 8 * 2 & 0xff;
        var g = Value >> 8 * 1 & 0xff;
        var b = Value >> 8 * 0 & 0xff;
        return (r, g, b);
    }
}
