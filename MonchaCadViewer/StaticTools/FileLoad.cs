﻿using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Tools.ILDA;
using CadProjectorSDK.UDP;
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
using System.Xml.Linq;
using ToGeometryConverter;
using ToGeometryConverter.Format;
using ToGeometryConverter.Object;
using static System.Drawing.Graphics;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.StaticTools
{
    public static class FileLoad
    {
        public static List<GCFormat> MyFormat = new List<GCFormat>
        {
            new SVG(),
            new DXF(),
            new GCODE(),
            //new DEXCeil(),
            new STL(),
            //new MetaFile(),
            //new JSON(),
            new GCFormat("Компас 3D", new string[2] { ".frw" , ".cdw"}) { ReadFile = GetKompas },
            //new GCFormat("JPG Image", new string[2] { "jpg" , "jpeg"}) { ReadFile = GetImage },
            new GCFormat("ILDA", new string[1] { ".ild" }){ ReadFile = GetILDA },
            new GCFormat("2CUT", new string[1] { ".2scn" }){ ReadFile = Get2CUT }
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
            projectorHub.Disconnect();

            //check path to setting file
            if (File.Exists(AppSt.Default.cl_moncha_path) == false || browse == true)
            {
                string str = FileLoad.BrowseMWS(); //select if not\
                AppSt.Default.cl_moncha_path = str;
                AppSt.Default.Save();
            }

            await projectorHub.Load(AppSt.Default.cl_moncha_path);
            //send path to hub class
            try
            {
               
            }
            catch
            {
                MessageBox.Show("Ошибка конфигурации!");
            }
        }

        public static async Task<UidObject> GetCliboard()
        {
            IDataObject Data = Clipboard.GetDataObject();

            string[] frm = Data.GetFormats();

            if (Clipboard.ContainsText() == true)
            {
                var text = Clipboard.GetData(DataFormats.Text) as string;

                SVG svg = new SVG();
                object obj = await svg.Parse(text);
                return await ConvertObject(obj);
            }
            return null;
        }

        public static async Task<UidObject> GetDrop(object obj, double step)
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
                                return await GetFilePath(fileLoc, step);
                            }
                        }
                    }
                }
                else if (dragEvent.Data.GetData(dragEvent.Data.GetFormats()[0]) is UidObject Scene)
                {
                    return Scene;
                }
            }
            return await Task.FromResult<UidObject>(null);
        }

        public static async Task<UidObject> GetFilePath(string FilePath, double step)
        {
            if (await FileLoad.GetObject(FilePath, step) is object obj)
            {
                if (await ConvertObject(obj) is UidObject uidObject)
                {
                    uidObject.Mirror = AppSt.Default.default_mirror;
                    uidObject.ScaleX = AppSt.Default.default_scale_x;
                    uidObject.ScaleY = AppSt.Default.default_scale_y;
                    return uidObject;
                }
            }
            return null;
        }

        public static async Task<UidObject> GetUDPString(string Filepath, bool Filename, double step)
        {
            return await GetFilePath(Filename == true ? $"{AppSt.Default.save_work_folder}\\{Filepath}" : Filepath, step);
        }

        public static async Task<UidObject> ConvertObject(object obj)
        {
            if (obj is GCCollection gCObjects)
            {
                if (await GCToCad.GetGroup(gCObjects) is CadGroup cadGroup)
                {
                    return cadGroup;
                }
                return null;
            }
            else if (obj is BitmapSource imageSource)
            {
                return new CadImage(imageSource);
            }
            else if (obj is UidObject uidObject)
            {
                return uidObject;
            }
            return await Task.FromResult<UidObject>(null);
        }

        private async static Task<object> GetObject(string Filename, double step)
        {
            GCFormat gCFormat = GCTools.GetConverter(Filename, MyFormat);
            object outobj = null;

            if (gCFormat != null)
            {
                outobj = await gCFormat.ReadFile?.Invoke(Filename, step);
            }

            if (outobj != null) 
                return outobj;
            else 
                return new GCCollection(string.Empty);
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

        private async static Task<object> GetILDA(string Filepath, double step)
        {
            return await IldaReader.ReadFile(Filepath);
        }

        private async static Task<object> Get2CUT(string Filepath, double step)
        {
            FileInfo fileInfo = new FileInfo(Filepath);
            XDocument xDocument = XDocument.Load(Filepath);
            XElement XObjects = xDocument.Element("Objects");

            CadGroup gCObjects = new CadGroup() { NameID = fileInfo.Name };

            foreach (XElement XObject in XObjects.Elements())
            {
                string item_path = XObject.Element("Path").Value;

                if (await FileLoad.GetObject(item_path, step) is GCCollection collection)
                {
                    if (await ConvertObject(collection) is UidObject uidObject)
                    {
                        uidObject.UpdateTransform(uidObject.Bounds, false, String.Empty);
                        uidObject.FileInfo = new FileInfo(item_path);
                        uidObject.MX = double.Parse(XObject.Element("X").Value);
                        uidObject.MY = double.Parse(XObject.Element("Y").Value);
                        uidObject.MZ = double.Parse(XObject.Element("Z").Value);
                        gCObjects.Add(uidObject);
                    }
                }
            }

            return gCObjects;
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
                _filter += $" | .{GetFormatString(formats[i])}";
            }

            _filter += " | All Files (*.*)|*.*";

            return _filter;
        }

        private static string GetFormatString(GCFormat format)
        {
            string formatstr = string.Empty;
            foreach (string frm in format.ShortName)
            {
                formatstr += $"*{frm};";
            }
            return $"{format.Name}({formatstr}) | {formatstr}";
        }


    }
}
