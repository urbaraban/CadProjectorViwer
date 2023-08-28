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
