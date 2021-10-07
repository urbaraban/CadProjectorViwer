using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
using CadProjectorViewer.Panels;
using KompasLib.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ToGeometryConverter;
using ToGeometryConverter.Format;
using ToGeometryConverter.Object;

namespace CadProjectorViewer.StaticTools
{
    public static class FileLoad
    {
        public static List<GCFormat> MyFormat = new List<GCFormat>
        {
            new SVG(),
            new DXF(),
            //new DEXCeil(),
            new STL(),
            //new ILD(),
            //new MetaFile(),
            //new JSON(),
            new GCFormat("Компас 3D", new string[2] { "frw" , "cdw"}) { ReadFile = GetKompas },
            new GCFormat("JPG Image", new string[2] { "jpg" , "jpeg"}) { ReadFile = GetImage }
        };



        private static Task<object> GetKompas(string Filepath, double step)
        {
            Process.Start(Filepath);
            return Task.FromResult<object>(null);
        }
        private async static Task<object> GetImage(string Filepath, double step)
        {
            return new BitmapImage(new Uri(Filepath));
        }

        public static List<GCFormat> GetFormatList()
        {
            List<GCFormat> Formats = new List<GCFormat>(MyFormat);

            List<string> _allformat = new List<string>();

            foreach (GCFormat format in Formats)
            {
                foreach (string frm in format.ShortName)
                {
                    _allformat.Add(frm);
                }
            }

            Formats.Insert(0, new GCFormat("All Format", _allformat.ToArray()));
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

        public async static Task<object> Get(string Filename)
        {
            GCFormat gCFormat = GCTools.GetConverter(Filename, MyFormat);

            object obj = await gCFormat.ReadFile?.Invoke(Filename, ProjectorHub.ProjectionSetting.PointStep.MX);

            if (obj != null) return obj;
            else return new GCCollection(string.Empty);
        }
    }
}
