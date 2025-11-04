using CadProjector.FileFormats.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CadProjector.FileFormats
{
    public class FileParserFactory
    {
        private readonly IEnumerable<IFileParser> parsers;

        public FileParserFactory(IEnumerable<IFileParser> parsers)
        {
            this.parsers = parsers;
        }

        public async Task<object> ParseFileAsync(string filePath)
        {
            var parser = GetParser(filePath);
            if (parser == null)
            {
                throw new NotSupportedException(
                  $"No parser found for file: {filePath}");
            }

            return await parser.ParseAsync(filePath);
        }

        public IFileParser GetParser(string filePath)
        {
            return parsers.FirstOrDefault(p => p.CanParse(filePath));
        }

        public string[] GetSupportedExtensions()
        {
            return parsers.SelectMany(p => p.SupportedExtensions)
                 .Distinct()
            .ToArray();
        }
    }
}