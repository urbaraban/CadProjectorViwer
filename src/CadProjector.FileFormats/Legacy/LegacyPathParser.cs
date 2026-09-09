using System.Globalization;
using CadProjector.Geometry.Primitives;

namespace CadProjector.FileFormats.Legacy;

/// <summary>Subset of WPF Path mini-language used in hybrid .2scn Geometry blobs.</summary>
public static class LegacyPathParser
{
    public static List<List<Point2>> Parse(string data)
    {
        var contours = new List<List<Point2>>();
        var current = new List<Point2>();
        var i = 0;
        var start = Point2.Zero;
        var last = Point2.Zero;
        var cmd = 'M';

        while (i < data.Length)
        {
            SkipSep(data, ref i);
            if (i >= data.Length) break;

            if (char.IsLetter(data[i]))
            {
                cmd = data[i];
                i++;
                SkipSep(data, ref i);
            }

            switch (cmd)
            {
                case 'M':
                case 'm':
                    if (current.Count > 0)
                    {
                        contours.Add(current);
                        current = [];
                    }
                    last = ReadPoint(data, ref i, cmd == 'm', last);
                    start = last;
                    current.Add(last);
                    cmd = cmd == 'M' ? 'L' : 'l';
                    break;
                case 'L':
                case 'l':
                    last = ReadPoint(data, ref i, cmd == 'l', last);
                    current.Add(last);
                    break;
                case 'H':
                case 'h':
                {
                    var x = ReadNumber(data, ref i);
                    last = new Point2(cmd == 'h' ? last.X + x : x, last.Y);
                    current.Add(last);
                    break;
                }
                case 'V':
                case 'v':
                {
                    var y = ReadNumber(data, ref i);
                    last = new Point2(last.X, cmd == 'v' ? last.Y + y : y);
                    current.Add(last);
                    break;
                }
                case 'C':
                case 'c':
                {
                    var relative = cmd == 'c';
                    ReadPoint(data, ref i, relative, last);
                    ReadPoint(data, ref i, relative, last);
                    last = ReadPoint(data, ref i, relative, last);
                    current.Add(last);
                    break;
                }
                case 'Z':
                case 'z':
                    if (current.Count > 0 && (current[0].X != last.X || current[0].Y != last.Y))
                        current.Add(start);
                    last = start;
                    break;
                default:
                    // Skip one number pair so we don't spin on unknown commands (A, Q, S, T).
                    if (i < data.Length && (char.IsDigit(data[i]) || data[i] is '-' or '.'))
                        ReadPoint(data, ref i, false, last);
                    else
                        i++;
                    break;
            }
        }

        if (current.Count > 0)
            contours.Add(current);
        return contours;
    }

    private static Point2 ReadPoint(string data, ref int i, bool relative, Point2 last)
    {
        var x = ReadNumber(data, ref i);
        var y = ReadNumber(data, ref i);
        return relative ? new Point2(last.X + x, last.Y + y) : new Point2(x, y);
    }

    private static double ReadNumber(string data, ref int i)
    {
        SkipSep(data, ref i);
        var start = i;
        if (i < data.Length && data[i] is '+' or '-') i++;
        while (i < data.Length && (char.IsDigit(data[i]) || data[i] is '.' or 'e' or 'E'))
        {
            if (data[i] is 'e' or 'E')
            {
                i++;
                if (i < data.Length && data[i] is '+' or '-') i++;
                continue;
            }
            i++;
        }

        var slice = data[start..i];
        return double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static void SkipSep(string data, ref int i)
    {
        while (i < data.Length && (char.IsWhiteSpace(data[i]) || data[i] is ','))
            i++;
    }
}
