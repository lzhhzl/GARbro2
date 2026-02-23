using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;

namespace GameRes.Formats.FrontierWorks
{
    [Export(typeof(ImageFormat))]
    public class ExtFormat : ImageFormat
    {
        public override string         Tag { get { return "EXT"; } }
        public override string Description { get { return "Frontier Works engine image format"; } }
        public override uint     Signature { get { return 0x30545845; } } // 'EXT0'

        public ExtFormat ()
        {
            Extensions = new string[] { "ext" };
        }

        public override ImageMetaData ReadMetaData (IBinaryStream stream)
        {
            stream.Position = 0;
            var signature = stream.ReadInt32 ();
            if (0x30545845 != signature)
                return null;
            stream.Position = 0xC;
            var width = stream.ReadUInt32 ();
            var height = stream.ReadUInt32 ();
            if (width > 0x1000 || height > 0x1000)
                return null;
            stream.Position = 0x24;
            var bpp = stream.ReadByte ();
            if (32 != bpp)
                return null;
            return new ImageMetaData
            {
                Width = width,
                Height = height,
                BPP = bpp,
            };
        }

        public override ImageData Read (IBinaryStream stream, ImageMetaData info)
        {
            stream.Position = 0x100;
            var pixels = new byte[info.Width*info.Height*4];
            if (pixels.Length != stream.Read (pixels, 0, pixels.Length))
                throw new EndOfStreamException ();
            return ImageData.Create (info, PixelFormats.Bgra32, null, pixels);
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotImplementedException ("ExtFormat.Write not implemented");
        }
    }
}
