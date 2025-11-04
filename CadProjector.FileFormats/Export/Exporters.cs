using System;
using System.IO;
using System.Threading.Tasks;
using CadProjector.Core.Objects;

namespace CadProjector.FileFormats.Export
{
    public interface IExporter
    {
        Task ExportAsync(IUidObject obj, string filePath);
        string[] SupportedExtensions { get; }
    }

    public class SvgExporter : IExporter
    {
        public string[] SupportedExtensions => new[] { ".svg" };

        public async Task ExportAsync(IUidObject obj, string filePath)
        {
            // TODO: Implement SVG export
            throw new NotImplementedException();
        }
    }

    public class DxfExporter : IExporter
    {
        public string[] SupportedExtensions => new[] { ".dxf" };

        public async Task ExportAsync(IUidObject obj, string filePath)
        {
            // TODO: Implement DXF export
            throw new NotImplementedException();
        }
    }
}