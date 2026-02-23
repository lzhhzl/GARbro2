using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using GameRes.Utility;

namespace GameRes.Formats.CatSystem {
    [Export(typeof(ArchiveFormat))]
    public class IrisPckOpener : ArchiveFormat {
        public override string         Tag { get { return "DAT/IRIS"; } }
        public override string Description { get { return "CatSystem for Android resource archive"; } }
        public override uint     Signature { get { return 0x53495249; } } // 'IRISPCK'
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen(ArcView file) {
            if (!file.View.AsciiEqual(4, "PCK"))
                return null;

            uint offset = 0x18;
            var dir = new List<Entry>();
            var name_buffer = new StringBuilder();

            while (offset < file.MaxOffset) {
                offset += 8;
                uint name_length = file.View.ReadUInt32(offset);

                offset += 8;
                name_buffer.Clear();
                for (int i = 0; i < name_length; i += 2) {
                    char c = (char)file.View.ReadUInt16(offset + i);
                    if (c == 0)
                        break;
                    name_buffer.Append(c);
                }
                var dirname = name_buffer.ToString().Replace("/", "\\");
                offset += name_length;

                offset += 4;
                int count = (int)file.View.ReadUInt32(offset);
                var dir_inner = new List<Entry>(count);

                offset += 0xC;
                uint prev = 0;
                for (int i = 0; i < count; i++) {
                    offset += 8;
                    uint size = file.View.ReadUInt32(offset);
                    uint padded_size = file.View.ReadUInt32(offset + 4);
                    name_length = file.View.ReadUInt32(offset + 8);
                    offset += 0x18;
                    name_buffer.Clear();
                    for (int j = 0; j < name_length; j += 2) {
                        char c = (char)file.View.ReadUInt16(offset + j);
                        if (c == 0)
                            break;
                        name_buffer.Append(c);
                    }
                    var basename = name_buffer.ToString();
                    var entry = Create<Entry>(Path.Combine(dirname, basename));
                    entry.Offset = prev;
                    entry.Size = size;
                    prev += padded_size;
                    offset += name_length;
                    dir_inner.Add(entry);
                }

                for (int i = 0; i < count; i++) {
                    dir_inner[i].Offset += offset;
                }
                offset += prev;
                dir.AddRange(dir_inner);
            }

            return new ArcFile(file, this, dir);
        }
    }
}
