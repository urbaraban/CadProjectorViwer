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
    public static class GetGC
    {
        private static GCFormat[] gCFormats = new GCFormat[]
        {
            new GCFormat("Компас 3D", new string[2] { "frw" , "cdw"}),
            new GCFormat("JPG Image", new string[2] { "jpg" , "jpeg"})
        };

        public static string GetFilter()
        {
            string _filter = string.Empty;
            string _allformat = string.Empty;

            List<GCFormat> Formats = new List<GCFormat>(ToGC.Formats);
            Formats.AddRange(gCFormats);

            foreach (GCFormat format in Formats)
            {
                foreach (string frm in format.ShortName)
                {
                    _allformat += $"*.{frm};";
                }
            }

            _filter += $"All Format ({_allformat}) | {_allformat}";

            foreach (GCFormat format in Formats)
            {
                _allformat = string.Empty;
                foreach (string frm in format.ShortName)
                {
                    _allformat += $"*.{frm};";
                }
                _filter += $" | {format.Name}({_allformat}) | {_allformat}";
            }

            _filter += " | All Files (*.*)|*.*";

            return _filter;
        }

        public async static Task<GCCollection> GCLoad(string Filename)
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
