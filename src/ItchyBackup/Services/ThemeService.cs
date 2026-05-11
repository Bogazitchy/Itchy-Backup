using System.Windows.Media;
using WpfApp   = System.Windows.Application;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;

namespace ItchyBackup.Services;

public static class ThemeService
{
    public static readonly string[] AccentPresets =
    {
        "#6C5CE7", // Mor (varsayılan)
        "#00CEC9", // Turkuaz
        "#00B894", // Yeşil
        "#FDCB6E", // Altın
        "#E17055", // Turuncu
        "#E84393", // Pembe
    };

    public static void Apply(string themeName, string accentHex)
    {
        var app = WpfApp.Current;
        if (app == null) return;
        ApplyAccent(app, accentHex);          // base accent colors first
        if (themeName == "Light")
            ApplyLight(app, accentHex);       // light theme overrides (including accent variants)
        else
            ApplyDark(app);
    }

    private static void ApplyDark(WpfApp app)
    {
        Set(app, "BgPrimary",    Brush(0x0C, 0x0B, 0x18));
        Set(app, "BgSecondary",  Brush(0x11, 0x10, 0x1E));
        Set(app, "BgTertiary",   Brush(0x1A, 0x19, 0x2E));
        Set(app, "BgQuaternary", Brush(0x22, 0x20, 0x3A));
        Set(app, "TextPrimary",  Brush(0xE8, 0xE8, 0xF0));
        Set(app, "TextSecondary",Brush(0x98, 0x98, 0xB0));
        Set(app, "TextTertiary", Brush(0x5A, 0x5A, 0x72));
        Set(app, "TextMuted",    Brush(0x3A, 0x3A, 0x50));
        Set(app, "BorderBrush",  Brush(0x22, 0x20, 0x5A));
        Set(app, "BorderHoverBrush", Brush(0x30, 0x28, 0x70));
        Set(app, "GlassBorder",  BrushA(0x22, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassHighlight", BrushA(0x12, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassSurface",  BrushA(0x08, 0xFF, 0xFF, 0xFF));
        Set(app, "BgCard", MakeGradient(
            new[] { (WpfColor.FromRgb(0x18, 0x18, 0x3C), 0.0),
                    (WpfColor.FromRgb(0x0E, 0x0E, 0x24), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBgBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0x0E, 0x0B, 0x1C), 0.0),
                    (WpfColor.FromRgb(0x0C, 0x0B, 0x18), 0.5),
                    (WpfColor.FromRgb(0x11, 0x0C, 0x22), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(1, 1)));
        Set(app, "TitleBarBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0x16, 0x15, 0x2E), 0.0),
                    (WpfColor.FromRgb(0x11, 0x10, 0x1E), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBorderBrush", Brush(0x4A, 0x38, 0x90));
    }

    private static void ApplyLight(WpfApp app, string accentHex)
    {
        // Soft violet/lavender palette — matches the app's purple identity
        Set(app, "BgPrimary",    Brush(0xF0, 0xEC, 0xFB)); // #F0ECFB — en açık, pencere içi
        Set(app, "BgSecondary",  Brush(0xE6, 0xDF, 0xF7)); // #E6DFF7 — sidebar, panel arkaplan
        Set(app, "BgTertiary",   Brush(0xDA, 0xD2, 0xF0)); // #DAD2F0 — hover, belirgin yüzey
        Set(app, "BgQuaternary", Brush(0xCE, 0xC5, 0xE8)); // #CEC5E8 — input, combobox
        Set(app, "TextPrimary",  Brush(0x1A, 0x10, 0x30)); // #1A1030 — koyu mor-lacivert, 10.5:1
        Set(app, "TextSecondary",Brush(0x48, 0x3A, 0x72)); // #483A72 — orta mor, 8.6:1
        Set(app, "TextTertiary", Brush(0x5A, 0x4E, 0x7A)); // #5A4E7A — soluk ama okunabilir, 6.3:1
        Set(app, "TextMuted",    Brush(0x7A, 0x6E, 0x94)); // #7A6E94 — en soluk, 3.9:1
        Set(app, "BorderBrush",  Brush(0xC4, 0xB8, 0xE2)); // #C4B8E2 — hafif mor kenar
        Set(app, "BorderHoverBrush", Brush(0xA8, 0x98, 0xD4)); // #A898D4 — hover kenar
        Set(app, "GlassBorder",  BrushA(0x28, 0x6C, 0x5C, 0xE7)); // accent-tinted glass
        Set(app, "GlassHighlight", BrushA(0x70, 0xFF, 0xFF, 0xFF));
        Set(app, "GlassSurface",  BrushA(0x40, 0xFF, 0xFF, 0xFF));
        Set(app, "BgCard", MakeGradient(
            new[] { (WpfColor.FromRgb(0xEC, 0xE6, 0xFA), 0.0),
                    (WpfColor.FromRgb(0xE4, 0xDD, 0xF5), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBgBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xEC, 0xE6, 0xFC), 0.0),
                    (WpfColor.FromRgb(0xE8, 0xE0, 0xF8), 0.5),
                    (WpfColor.FromRgb(0xEE, 0xE6, 0xFF), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(1, 1)));
        Set(app, "TitleBarBrush", MakeGradient(
            new[] { (WpfColor.FromRgb(0xDC, 0xD4, 0xF5), 0.0),
                    (WpfColor.FromRgb(0xD5, 0xCC, 0xF0), 1.0) },
            new WpfPoint(0, 0), new WpfPoint(0, 1)));
        Set(app, "WindowBorderBrush", Brush(0xAA, 0x92, 0xDC)); // #AA92DC

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
