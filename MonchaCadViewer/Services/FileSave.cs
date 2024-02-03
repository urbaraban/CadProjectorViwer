using CadProjectorSDK.Device;
using CadProjectorSDK.Tools.ILDA;
using Microsoft.Win32;
using System.Collections.Generic;

namespace CadProjectorViewer.Services
{
    internal static class FileSave
    {
        public static void ILDASave(LProjector[] lProjectors)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "(*.ild)|*.ild| All Files (*.*)|*.*";
            saveFileDialog.FileName = "export";
            saveFileDialog.ShowDialog();
            IldaWriter ildaWriter = new IldaWriter();

            if (saveFileDialog.FileName != string.Empty)
            {
                foreach(var projector in lProjectors)
                {
                    var filename = saveFileDialog.FileName.Replace(".ild", string.Empty);
                    ildaWriter.Write(($"{filename}_{projector.IpAddress}.ild"),
                    new List<IldaFrame>() {
                            projector.IldFrame
                        ?? new IldaFrame() }, 5);
                }
            }
        }
    }
}
