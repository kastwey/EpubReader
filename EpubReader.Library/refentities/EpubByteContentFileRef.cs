﻿using System.Threading.Tasks;

namespace EpubReader.Library
{
    public class EpubByteContentFileRef : EpubContentFileRef
    {
        public EpubByteContentFileRef(EpubBookRef epubBookRef)
            : base(epubBookRef)
        {
        }

        public byte[] ReadContent()
        {
            return ReadContentAsBytes();
        }

        public Task<byte[]> ReadContentAsync()
        {
            return ReadContentAsBytesAsync();
        }
    }
}
