using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using GameRes.Utility;

namespace GameRes.Formats.Duke
{
    [Export(typeof(ArchiveFormat))]
    public class DatOpener : ArchiveFormat
    {
        public override string         Tag { get { return "DAT/DUKE"; } }
        public override string Description { get { return "Duke resource archive"; } }
        public override uint     Signature { get { return 0; } }
        public override bool  IsHierarchic { get { return false; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.Name.HasExtension (".dat"))
                return null;

            int count = (int)(file.View.ReadUInt32 (0) ^ 0xfa261efb);
            if (!IsSaneCount (count))
                return null;

            uint index_offset = 4;
            uint data_offset = index_offset + (uint)count * 0x28u;
            var dir = new List<Entry> (count);
            for (int i = 0; i < count; ++i)
            {
                var buffer = file.View.ReadBytes (index_offset, 0x20);
                for (int counter = 0; counter < 0x20; counter++)
                {
                    buffer[counter] = (byte)(buffer[counter] ^ counter * 5 + 172);
                }
                var name = Binary.GetCString (buffer, 0);
                if (string.IsNullOrWhiteSpace (name))
                    return null;
                var entry = FormatCatalog.Instance.Create<Entry> (name);
                entry.Size   = (uint)(file.View.ReadUInt32 (index_offset+0x20) ^ 0xfa261efb);
                entry.Offset = file.View.ReadUInt32 (index_offset+0x24);
                if (entry.Offset < data_offset || !entry.CheckPlacement (file.MaxOffset))
                    return null;
                dir.Add (entry);
                index_offset += 0x28;
            }
            return new ArcFile (file, this, dir);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var data = arc.File.View.ReadBytes (entry.Offset, entry.Size);
            var name = Encodings.cp932.GetBytes (entry.Name);
            int length = name.Length;
            for (int i = 0; i < entry.Size && i < 0x2c00; i += length)
            {
                for (int j = 0; j < length && j < entry.Size - i; j++)
                {
                    data[i + j] = (byte)(data[i + j] ^ name[j] + i + j);
                }
            }
            return new BinMemoryStream (data);
        }
    }
}
