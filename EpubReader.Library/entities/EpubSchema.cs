using EpubReader.Library.Schema.Navigation;
using EpubReader.Library.Schema.Opf;

namespace EpubReader.Library
{
    public class EpubSchema
    {
        public EpubPackage Package { get; set; }
        public EpubNavigation Navigation { get; set; }
        public string ContentDirectoryPath { get; set; }
    }
}
