using System.Windows.Media;
using WpfApp   = System.Windows.Application;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;

namespace ItchyBackup.Services;

public static class ThemeService
{
    public static readonly string[] AccentPresets =
    {
        "#007A4D", // Itchy yeşili (varsayılan)
        "#00B875", // Parlak yeşil
        "#00462D", // Koyu yeşil
        "#00CEC9", // Turkuaz
        "#FDCB6E", // Altın
        "#E17055", // Turuncu
    };

    public static void Apply(string themeName, string accentHex)
    {
        var app = WpfApp.Current;
        if (app == null) return;
        ApplyAccent(app, accentHex);          // base accent colors first
        if (themeName == "Service")
            ApplyService(app);
        else if (themeName == "Light")
            ApplyLight(app, accentHex);       // light theme overrides (including accent variants)
        else
            ApplyDark(app);
    }

    private static void ApplyService(WpfApp app)
    {
        Set(app, "BgPrimary",    Brush(0xF7, 0xF8, 0xFA));
        Set(app, "BgSecondary",  Brush(0xEC, 0xF0, 0xF3));
        Set(app, "BgTertiary",   Brush(0xE0, 0xE7, 0xEC));
        Set(app, "BgQuaternary", Brush(0xD4, 0xDE, 0xE6));
        Set(app, "TextPrimary",  Brush(0x14, 0x1A, 0x22));
        Set(app, "TextSecondary",Brush(0x3B, 0x4A, 0x5A));
        Set(app, "TextTertiary", Brush(0x63, 0x72, 0x82));
        Set(app, "TextMuted",    Brush(0x8A, 0x95, 0xA3));
        Set(app, "BorderBrush",  Brush(0xC9, 0xD4, 0xDD));
        Set(app, "BorderHoverBrush", Brush(0x8D, 0xA1, 0xB2));
        Set(app, "GlassBorder",  BrushA(0x30, 0x14, 0x1A, 0x22));
        Set(app, "GlassHighlight", BrushA(0x80, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassSurface",  BrushA(0x40, 0xFF, 0xFF, 0xFF));
        Set(app, "Accent",       Brush(0x00, 0x7A, 0x4D));
        Set(app, "AccentLight",  Brush(0x00, 0x7A, 0x4D));
        Set(app, "AccentDark",   Brush(0x00, 0x46, 0x2D));
        Set(app, "AccentSubtle", BrushA(0x28, 0x00, 0x7A, 0x4D));
        Set(app, "BgCard", MakeGradient(
            new[] { (WpfColor.FromRgb(0xFF, 0xFF, 0xFF), 0.0),
                    (WpfColor.FromRgb(0xF3, 0xF6, 0xF8), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBgBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xF8, 0xFA, 0xFC), 0.0),
                    (WpfColor.FromRgb(0xEF, 0xF3, 0xF6), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(1, 1)));
        Set(app, "TitleBarBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xEC, 0xF0, 0xF3), 0.0),
                    (WpfColor.FromRgb(0xE2, 0xE8, 0xEE), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBorderBrush", Brush(0x9A, 0xAD, 0xBD));
    }

    private static void ApplyDark(WpfApp app)
    {
        Set(app, "BgPrimary",    Brush(0x11, 0x13, 0x15));
        Set(app, "BgSecondary",  Brush(0x18, 0x1B, 0x1E));
        Set(app, "BgTertiary",   Brush(0x20, 0x24, 0x28));
        Set(app, "BgQuaternary", Brush(0x29, 0x2E, 0x33));
        Set(app, "TextPrimary",  Brush(0xF2, 0xF4, 0xF5));
        Set(app, "TextSecondary",Brush(0xB2, 0xB8, 0xBD));
        Set(app, "TextTertiary", Brush(0x74, 0x7C, 0x83));
        Set(app, "TextMuted",    Brush(0x4C, 0x53, 0x59));
        Set(app, "BorderBrush",  Brush(0x30, 0x36, 0x3B));
        Set(app, "BorderHoverBrush", Brush(0x4A, 0x55, 0x5D));
        Set(app, "GlassBorder",  BrushA(0x22, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassHighlight", BrushA(0x12, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassSurface",  BrushA(0x08, 0xFF, 0xFF, 0xFF));
        Set(app, "BgCard", MakeGradient(
            new[] { (WpfColor.FromRgb(0x20, 0x24, 0x28), 0.0),
                    (WpfColor.FromRgb(0x18, 0x1B, 0x1E), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBgBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0x11, 0x13, 0x15), 0.0),
                    (WpfColor.FromRgb(0x14, 0x17, 0x19), 0.5),
                    (WpfColor.FromRgb(0x17, 0x1B, 0x1E), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(1, 1)));
        Set(app, "TitleBarBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0x1B, 0x1F, 0x22), 0.0),
                    (WpfColor.FromRgb(0x18, 0x1B, 0x1E), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBorderBrush", Brush(0x34, 0x3B, 0x40));
    }

    private static void ApplyLight(WpfApp app, string accentHex)
    {
        Set(app, "BgPrimary",    Brush(0xF4, 0xF5, 0xF6));
        Set(app, "BgSecondary",  Brush(0xFF, 0xFF, 0xFF));
        Set(app, "BgTertiary",   Brush(0xEA, 0xED, 0xEF));
        Set(app, "BgQuaternary", Brush(0xDF, 0xE3, 0xE6));
        Set(app, "TextPrimary",  Brush(0x18, 0x1C, 0x1F));
        Set(app, "TextSecondary",Brush(0x45, 0x4D, 0x53));
        Set(app, "TextTertiary", Brush(0x70, 0x78, 0x7E));
        Set(app, "TextMuted",    Brush(0x9A, 0xA1, 0xA6));
        Set(app, "BorderBrush",  Brush(0xD5, 0xDA, 0xDE));
        Set(app, "BorderHoverBrush", Brush(0xA8, 0xB1, 0xB7));
        Set(app, "GlassBorder",  BrushA(0x28, 0x00, 0x7A, 0x4D));
        Set(app, "GlassHighlight", BrushA(0x70, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassSurface",  BrushA(0x40, 0xFF, 0xFF, 0xFF));
        Set(app, "BgCard", MakeGradient(
            new[] { (WpfColor.FromRgb(0xFF, 0xFF, 0xFF), 0.0),
                    (WpfColor.FromRgb(0xF5, 0xF6, 0xF7), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBgBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xF4, 0xF5, 0xF6), 0.0),
                    (WpfColor.FromRgb(0xF1, 0xF3, 0xF4), 0.5),
                    (WpfColor.FromRgb(0xF7, 0xF8, 0xF9), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(1, 1)));
        Set(app, "TitleBarBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xFF, 0xFF, 0xFF), 0.0),
                    (WpfColor.FromRgb(0xF0, 0xF2, 0xF3), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBorderBrush", Brush(0xC8, 0xCE, 0xD2));

        // Açık temada ikon/metin görünürlüğü için accent varyantlarını düzelt.
        // ApplyAccent'ten gelen AccentLight çok açık renk; açık zemin üzerinde
        // kontrast 1.8:1'e düşer. Koyu/tam renk ile override ediyoruz.
        try
        {
            var c = (WpfColor)System.Windows.Media.ColorConverter.ConvertFromString(accentHex);
            Set(app, "AccentLight",  new SolidColorBrush(c));               // tam accent rengi — açık bg'de okunabilir
            Set(app, "AccentBright", new SolidColorBrush(Darken(c, 0.06f)));
            Set(app, "AccentSubtle", BrushA(0x44, c.R, c.G, c.B));         // daha opak ikon kutuları
        }
        catch { }
    }

    private static void ApplyAccent(WpfApp app, string hex)
    {
        try
        {
            var c = (WpfColor)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            Set(app, "Accent",       new SolidColorBrush(c));
            Set(app, "AccentLight",  new SolidColorBrush(Lighten(c, 0.28f)));
            Set(app, "AccentDark",   new SolidColorBrush(Darken(c, 0.22f)));
            Set(app, "AccentGlow",   new SolidColorBrush(c));
            Set(app, "AccentBright", new SolidColorBrush(Lighten(c, 0.14f)));
            Set(app, "AccentSubtle", BrushA(0x22, c.R, c.G, c.B));
            Set(app, "NeonPurple",   new SolidColorBrush(Lighten(c, 0.18f)));
            Set(app, "NeonPurpleColor", c);
            Set(app, "ProgressFillBrush", MakeGradient(
                new[] { (Darken(c, 0.20f), 0.0),
                        (c,                0.6),
                        (Lighten(c, 0.22f), 1.0) },
                new WpfPoint(0, 0), new WpfPoint(1, 0)));
        }
        catch { }
    }

    private static void Set(WpfApp app, string key, object value)
        => app.Resources[key] = value;

    private static SolidColorBrush Brush(byte r, byte g, byte b)
        => new(WpfColor.FromRgb(r, g, b));

    private static SolidColorBrush BrushA(byte a, byte r, byte g, byte b)
        => new(WpfColor.FromArgb(a, r, g, b));

    private static LinearGradientBrush MakeGradient(
        (WpfColor c, double o)[] stops, WpfPoint start, WpfPoint end)
    {
        var col = new GradientStopCollection(stops.Select(s => new GradientStop(s.c, s.o)));
        return new LinearGradientBrush(col, start, end);
    }

    private static WpfColor Lighten(WpfColor c, float amount) => WpfColor.FromRgb(
        (byte)Math.Min(255, (int)(c.R + 255 * amount)),
        (byte)Math.Min(255, (int)(c.G + 255 * amount)),
        (byte)Math.Min(255, (int)(c.B + 255 * amount)));

    private static WpfColor Darken(WpfColor c, float amount) => WpfColor.FromRgb(
        (byte)Math.Max(0, (int)(c.R - 255 * amount)),
        (byte)Math.Max(0, (int)(c.G - 255 * amount)),
        (byte)Math.Max(0, (int)(c.B - 255 * amount)));
}
