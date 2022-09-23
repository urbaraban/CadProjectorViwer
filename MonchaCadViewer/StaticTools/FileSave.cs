using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Config;
using CadProjectorSDK.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CadProjectorViewer.StaticTools
{
    internal static class FileSave
    {
        public static void SaveScene(ProjectionScene uidObjects, string path)
        {
            XDocument xDocument = new XDocument();

            XElement XObjects = new XElement("Objects");
            foreach (UidObject uidObject in uidObjects)
            {
                if (uidObject.FileInfo != null)
                {
                    XObjects.Add(new XElement("UidObject",
                        new XElement("Path", uidObject.FileInfo.FullName),
                        new XElement("X", uidObject.MX.ToString()),
                        new XElement("Y", uidObject.MY.ToString()),
                        new XElement("Z", uidObject.MZ.ToString())
                        ));
                }
            }
            xDocument.Add(XObjects);

            xDocument.Save(path);
        }
    }
}
