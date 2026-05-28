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
        Set(app, "BgPrimary",    Brush(0x07, 0x11, 0x0D));
        Set(app, "BgSecondary",  Brush(0x08, 0x1A, 0x14));
        Set(app, "BgTertiary",   Brush(0x0D, 0x24, 0x1C));
        Set(app, "BgQuaternary", Brush(0x12, 0x32, 0x28));
        Set(app, "TextPrimary",  Brush(0xE8, 0xE8, 0xF0));
        Set(app, "TextSecondary",Brush(0x9A, 0xB4, 0xAA));
        Set(app, "TextTertiary", Brush(0x5F, 0x7E, 0x72));
        Set(app, "TextMuted",    Brush(0x35, 0x52, 0x48));
        Set(app, "BorderBrush",  Brush(0x12, 0x3B, 0x2C));
        Set(app, "BorderHoverBrush", Brush(0x17, 0x60, 0x44));
        Set(app, "GlassBorder",  BrushA(0x22, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassHighlight", BrushA(0x12, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassSurface",  BrushA(0x08, 0xFF, 0xFF, 0xFF));
        Set(app, "BgCard", MakeGradient(
            new[] { (WpfColor.FromRgb(0x0D, 0x28, 0x1F), 0.0),
                    (WpfColor.FromRgb(0x06, 0x17, 0x11), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBgBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0x06, 0x12, 0x0E), 0.0),
                    (WpfColor.FromRgb(0x07, 0x11, 0x0D), 0.5),
                    (WpfColor.FromRgb(0x08, 0x20, 0x18), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(1, 1)));
        Set(app, "TitleBarBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0x0A, 0x1F, 0x18), 0.0),
                    (WpfColor.FromRgb(0x08, 0x1A, 0x14), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBorderBrush", Brush(0x00, 0x6B, 0x43));
    }

    private static void ApplyLight(WpfApp app, string accentHex)
    {
        Set(app, "BgPrimary",    Brush(0xEC, 0xF7, 0xF1));
        Set(app, "BgSecondary",  Brush(0xDF, 0xEE, 0xE7));
        Set(app, "BgTertiary",   Brush(0xCE, 0xE2, 0xD8));
        Set(app, "BgQuaternary", Brush(0xBF, 0xD5, 0xCA));
        Set(app, "TextPrimary",  Brush(0x0C, 0x24, 0x1A));
        Set(app, "TextSecondary",Brush(0x2A, 0x54, 0x42));
        Set(app, "TextTertiary", Brush(0x4E, 0x72, 0x63));
        Set(app, "TextMuted",    Brush(0x70, 0x8D, 0x80));
        Set(app, "BorderBrush",  Brush(0xB2, 0xD0, 0xC2));
        Set(app, "BorderHoverBrush", Brush(0x78, 0xAE, 0x95));
        Set(app, "GlassBorder",  BrushA(0x28, 0x00, 0x7A, 0x4D));
        Set(app, "GlassHighlight", BrushA(0x70, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassSurface",  BrushA(0x40, 0xFF, 0xFF, 0xFF));
        Set(app, "BgCard", MakeGradient(
            new[] { (WpfColor.FromRgb(0xF4, 0xFB, 0xF7), 0.0),
                    (WpfColor.FromRgb(0xE5, 0xF1, 0xEB), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBgBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xF3, 0xFB, 0xF7), 0.0),
                    (WpfColor.FromRgb(0xE9, 0xF4, 0xEF), 0.5),
                    (WpfColor.FromRgb(0xF5, 0xFF, 0xFA), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(1, 1)));
        Set(app, "TitleBarBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xD9, 0xEC, 0xE4), 0.0),
                    (WpfColor.FromRgb(0xCD, 0xE3, 0xD8), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBorderBrush", Brush(0x66, 0xA8, 0x88));

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
