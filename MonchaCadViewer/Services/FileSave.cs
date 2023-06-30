using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device;
using CadProjectorSDK.Scenes;
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
            //SaveFileDialog saveFileDialog = new SaveFileDialog();
            //saveFileDialog.Filter = "(*.ild)|*.ild| All Files (*.*)|*.*";
            //saveFileDialog.FileName = "export";
            //saveFileDialog.ShowDialog();
            //IldaWriter ildaWriter = new IldaWriter();

            //if (saveFileDialog.FileName != string.Empty)
            //{
            //    for (int i = 0; i < lProjectors.Length; i += 1)
            //    {
            //        IList<IRenderedObject> elements = GraphExtensions.SolidVectors(lProjectors[i].RenderObjects, lProjectors[i]);
            //        if (lProjectors[i].Optimized == true && elements.Count > 0)
            //        {
            //            //vectorLines = VectorLinesCollection.Optimize(vectorLines);
            //            elements = GraphExtensions.FindShortestCollection(
            //                elements, lProjectors[i].ProjectionSetting.PathFindDeep, lProjectors[i].ProjectionSetting.FindSolidElement);
            //        }
            //        var vectorLine = GraphExtensions.GetVectorLines(elements);

            //        ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}_{lProjectors[i].IPAddress}.ild"),
            //            new List<LFrame>() {
            //                LFrameConverter.SolidLFrame(vectorLine, lProjectors[i])
            //            ?? new LFrame() }, 5);
            //    }
            //}
        }
    }
}
