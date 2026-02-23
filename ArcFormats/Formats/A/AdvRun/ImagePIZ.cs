using System;
using System.ComponentModel.Composition;
using System.IO;
using GameRes.Compression;

namespace GameRes.Formats.AdvRun
{
    [Export(typeof(ImageFormat))]
    public class PizFormat : ImageFormat
    {
        public override string         Tag { get { return "PIZ"; } }
        public override string Description { get { return "ADVRUN compressed bitmap"; } }
        public override uint     Signature { get { return 0; } }
        public override bool      CanWrite { get { return false; } }

        public PizFormat ()
        {
            Extensions = new string[] { "piz" };
        }

        public override ImageMetaData ReadMetaData (IBinaryStream stream)
        {
            if (stream.Signature + 4 != stream.Length)
                return null;
            stream.Position = 4;
            using (var lz = new ZLibStream (stream.AsStream, CompressionMode.Decompress))
            {
                using (var bmp = new BinaryStream (lz, stream.Name))
                    return Bmp.ReadMetaData (bmp);
            }
        }

        public override ImageData Read (IBinaryStream stream, ImageMetaData info)
        {
            stream.Position = 4;
            using (var lz = new ZLibStream (stream.AsStream, CompressionMode.Decompress))
            {
                using (var bmp = new BinaryStream (lz, stream.Name))
                    return Bmp.Read (bmp, info);
            }
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotImplementedException ("PizFormat.Write not implemented");
        }
    }
}
