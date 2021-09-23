using CadProjectorSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToGeometryConverter;
using ToGeometryConverter.Format;
using ToGeometryConverter.Object;

namespace CadProjectorViewer.StaticTools
{
    public static class FileLoad
    {
        private static List<GCFormat> MyFormat = new List<GCFormat>
        {
            new GCFormat("Компас 3D", new string[2] { "frw" , "cdw"}),
            new GCFormat("JPG Image", new string[2] { "jpg" , "jpeg"}),
        };

        public static List<GCFormat> GetFormatList()
        {
            List<GCFormat> Formats = new List<GCFormat>(ToGC.Formats);
            Formats.AddRange(MyFormat);

            List<string> _allformat = new List<string>();

            foreach (GCFormat format in Formats)
            {
                foreach (string frm in format.ShortName)
                {
                    _allformat.Add(frm);
                }
            }

            Formats.Add(new GCFormat("All Format", _allformat.ToArray()));
            return Formats;
        }

        public static string GetFilter()
        {
            List<GCFormat> formats = GetFormatList();

            string _filter = GetFormatString(formats[0]);

            for (int i = 1; i < formats.Count; i += 1)
            {
                _filter += $" | {GetFormatString(formats[i])}";
            }

            _filter += " | All Files (*.*)|*.*";

            return _filter;
        }

        private static string GetFormatString(GCFormat format)
        {
            string formatstr = string.Empty;
            foreach (string frm in format.ShortName)
            {
                formatstr += $"*.{frm};";
            }
            return $"{format.Name}({formatstr}) | {formatstr}";
        }

        public async static Task<GCCollection> Get(string Filename)
        {
            GCFormat gCFormat = ToGC.GetConverter(Filename);
            if (gCFormat is IReadWrite format)
            {
                return await format.GetAsync(Filename, ProjectorHub.ProjectionSetting.PointStep.MX);
            }
            return new GCCollection(string.Empty);
        }
    }
}
