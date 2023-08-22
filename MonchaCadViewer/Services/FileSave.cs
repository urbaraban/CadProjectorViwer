using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Tools.ILDA;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CadProjectorViewer.Services
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

        public static void ILDASave(LProjector[] lProjectors)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "(*.ild)|*.ild| All Files (*.*)|*.*";
            saveFileDialog.FileName = "export";
            saveFileDialog.ShowDialog();
            IldaWriter ildaWriter = new IldaWriter();

            if (saveFileDialog.FileName != string.Empty)
            {
                for (int i = 0; i < lProjectors.Length; i += 1)
                {
                    ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}_{lProjectors[i].IPAddress}.ild"),
                        new List<IldaFrame>() {
                            lProjectors[i].IldFrame
                        ?? new IldaFrame() }, 5);
                }
            }
        }
    }
}
