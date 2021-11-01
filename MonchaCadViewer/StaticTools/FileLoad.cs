using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.Panels;
using KompasLib.Tools;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ToGeometryConverter;
using ToGeometryConverter.Format;
using ToGeometryConverter.Object;
using AppSt = CadProjectorViewer.Properties.Settings;

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

        private static string BrowseMWS()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Moncha (.mws)|*.mws|All Files (*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                return fileDialog.FileName;
            }
            return string.Empty;
        }

        public static async void LoadMoncha(ProjectorHub projectorHub, bool browse)
        {
            projectorHub.Play = false;

            //check path to setting file
            if (File.Exists(AppSt.Default.cl_moncha_path) == false || browse == true)
            {
                string str = FileLoad.BrowseMWS(); //select if not\
                AppSt.Default.cl_moncha_path = str;
                AppSt.Default.Save();
            }
            projectorHub.Disconnect();

            //send path to hub class
            try
            {
                await projectorHub.Load(AppSt.Default.cl_moncha_path);
            }
            catch
            {
                MessageBox.Show("Ошибка конфигурации!");
            }
        }

        public static async Task<ProjectionScene> GetCliboard(string clip)
        {
            SVG svg = new SVG();
            object obj = await svg.ParseClip(clip);
            return new ProjectionScene(await ConvertObject(obj));
        }

        public static async Task<ProjectionScene> GetScene(object obj)
        {
            if (obj is DragEventArgs dragEvent)
            {
                if (dragEvent.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    if (dragEvent.Data.GetData(DataFormats.FileDrop) is string[] strings) {
                        foreach (string fileLoc in strings)
                        {
                            if (File.Exists(fileLoc))
                            {
                                return await GetFilePath(fileLoc);
                            }
                        }
                    }
                }
                else if (dragEvent.Data.GetData(dragEvent.Data.GetFormats()[0]) is ProjectionScene Scene)
                {
                    return dragEvent.Effects == DragDropEffects.Copy ? Scene.Clone() : Scene;
                }
            }
            return await Task.FromResult<ProjectionScene>(null);
        }

        public static async Task<ProjectionScene> GetFilePath(string FilePath)
        {
            return new ProjectionScene(await ConvertObject(await FileLoad.GetObject(FilePath)));
        }

        private static async Task<UidObject> ConvertObject(object obj)
        {
            if (obj is GCCollection gCObjects)
            {
                return await GCToCad.GetGroup(gCObjects);
            }
            else if (obj is BitmapSource imageSource)
            {
                return new CadImage(imageSource);
            }
            return await Task.FromResult<UidObject>(null);
        }

        private async static Task<object> GetObject(string Filename)
        {
            GCFormat gCFormat = GCTools.GetConverter(Filename, MyFormat);

            object obj = await gCFormat.ReadFile?.Invoke(Filename, ProjectorHub.ProjectionSetting.PointStep.MX);

            if (obj != null) return obj;
            else return new GCCollection(string.Empty);
        }

        /// <summary>
        /// Load Kompas 3D file
        /// </summary>
        /// <param name="Filepath"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        private static Task<object> GetKompas(string Filepath, double step)
        {
            Process.Start(Filepath);
            return Task.FromResult<object>(null);
        }
        /// <summary>
        /// Load Jpg image
        /// </summary>
        /// <param name="Filepath"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        private async static Task<object> GetImage(string Filepath, double step)
        {
            return new BitmapImage(new Uri(Filepath));
        }

        /// <summary>
        /// Get Format list for search or other...
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Get filter for OpenFileDialog
        /// </summary>
        /// <returns></returns>
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


    }
}
