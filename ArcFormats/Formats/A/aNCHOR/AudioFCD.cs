using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace GameRes.Formats.Anchor
{
    [Export(typeof(AudioFormat))]
    public class FcdAudio : AudioFormat
    {
        public override string         Tag { get { return "FCD"; } }
        public override string Description { get { return "AGES Mk2 audio format"; } }
        public override uint     Signature { get { return 0x00444346; } } // 'FCD\x00'

        public FcdAudio ()
        {
            Extensions = new string[] { "fcd" };
        }

        public override SoundInput TryOpen (IBinaryStream file)
        {
            file.Position = 4;
            // guess: big endian, version=2, type=0 (ogg), offset=0xC
            byte[] data = { 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x4F, 0x67, 0x67, 0x53 };
            if (!file.ReadBytes (0x0C).SequenceEqual (data))
                throw new NotSupportedException();
            return new OggInput (new StreamRegion (file.AsStream, 0x0C));
        }
    }
}
