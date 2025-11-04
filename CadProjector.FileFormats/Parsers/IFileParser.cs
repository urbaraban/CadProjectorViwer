using System.Threading.Tasks;

namespace CadProjector.FileFormats.Parsers
{
public interface IFileParser
    {
        bool CanParse(string filePath);
   Task<object> ParseAsync(string filePath);
        string[] SupportedExtensions { get; }
    }
}