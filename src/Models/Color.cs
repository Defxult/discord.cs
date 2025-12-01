namespace Discord.Models;

/// <summary>
/// Represents a Discord color.
/// </summary>
public struct Color : IEquatable<Color>
{
    /// <summary>
    /// Returns a color with its value set to <c>0x5865f2</c>.
    /// </summary>
    public static readonly Color Blurple = new(0x5865F2);
    
    /// <summary>
    /// Returns a color with its value set to <c>0xEB459E</c>.
    /// </summary>
    public static readonly Color Fuchsia = new(0xEB459E);
    
    /// <summary>
    /// Returns a color with its value set to <c>0xFC0303</c>.
    /// </summary>
    public static readonly Color Red = new(0xFC0303);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x910101</c>.
    /// </summary>
    public static readonly Color DarkRed = new(0x910101);
    
    /// <summary>
    /// Returns a color with its value set to <c>0xFF992B</c>.
    /// </summary>
    public static readonly Color Orange = new(0xFF992B);
    
    /// <summary>
    /// Returns a color with its value set to <c>0xFFDC2B</c>.
    /// </summary>
    public static readonly Color Yellow = new(0xFFDC2B);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x2BFF32</c>.
    /// </summary>
    public static readonly Color Green = new(0x2BFF32);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x026105</c>.
    /// </summary>
    public static readonly Color DarkGreen = new(0x026105);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x36B1D6</c>.
    /// </summary>
    public static readonly Color SkyBlue = new(0x36B1D6);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x1021E3</c>.
    /// </summary>
    public static readonly Color DarkBlue = new(0x1021E3);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x8F44F2</c>.
    /// </summary>
    public static readonly Color Purple = new(0x8F44F2);
    
    /// <summary>
    /// Returns a color with its value set to <c>0xFCA7F0</c>.
    /// </summary>
    public static readonly Color Pink = new(0xFCA7F0);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x000001</c>.
    /// </summary>
    public static readonly Color Black = new(0x000001);
    
    /// <summary>
    /// Returns a color with its value set to <c>0xFFFFFF</c>.
    /// </summary>
    public static readonly Color White = new(0xFFFFFF);
    
    /// <summary>
    /// Returns a color with its value set to <c>0xA6A6A6</c>.
    /// </summary>
    public static readonly Color Gray = new(0xA6A6A6);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x2F3136</c>.
    /// </summary>
    public static readonly Color DarkTheme = new(0x2F3136);
    
    /// <summary>
    /// Returns a color with its value set to <c>0x008080</c>.
    /// </summary>
    public static readonly Color Teal = new(0x008080);

    /// <summary>
    /// Maximum a color value can be (lowest is 0).
    /// </summary>
    public const int Max = 0xFFFFFF;

    /// <summary>
    /// Raw color value.
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
    /// Initializes a new color using a hexadecimal value.
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
