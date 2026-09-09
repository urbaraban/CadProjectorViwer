using System.Globalization;
using System.Xml.Linq;
using CadProjector.Geometry.Primitives;

namespace CadProjector.FileFormats.Legacy;

internal static class LegacyXml
{
    public static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    public static string Attr(XElement el, string name, string fallback = "") =>
        (string?)el.Attribute(name) ?? fallback;

    public static string Child(XElement parent, string name, string fallback = "") =>
        parent.Element(name)?.Value ?? fallback;

    public static double Num(XElement parent, string name, double fallback = 0)
    {
        var raw = parent.Element(name)?.Value;
        return ParseDouble(raw, fallback);
    }

    public static double AttrNum(XElement el, string name, double fallback = 0) =>
        ParseDouble((string?)el.Attribute(name), fallback);

    public static bool Flag(XElement parent, string name, bool fallback = false)
    {
        var raw = parent.Element(name)?.Value;
        return bool.TryParse(raw, out var v) ? v : fallback;
    }

    public static double ParseDouble(string? raw, double fallback = 0)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;
        raw = raw.Trim().Trim('"');
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var inv))
            return inv;
        if (double.TryParse(raw.Replace('.', ','), NumberStyles.Float, CultureInfo.GetCultureInfo("ru-RU"), out var ru))
            return ru;
        return fallback;
    }

    public static bool TryParsePoint(string? raw, out Point3 p)
    {
        p = Point3.Zero;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var parts = raw.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return false;
        p = new Point3(
            ParseDouble(parts[0]),
            ParseDouble(parts[1]),
            parts.Length > 2 ? ParseDouble(parts[2]) : 0);
        return true;
    }

    public static string TypeName(XElement el)
    {
        var xsi = (string?)el.Attribute(Xsi + "type");
        if (!string.IsNullOrWhiteSpace(xsi))
        {
            var colon = xsi.LastIndexOf(':');
            return colon >= 0 ? xsi[(colon + 1)..] : xsi;
        }

        var type = Attr(el, "Type");
        return string.IsNullOrWhiteSpace(type) ? el.Name.LocalName : type;
    }
}
