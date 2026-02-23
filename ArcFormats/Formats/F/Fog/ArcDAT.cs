using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using GameRes.Utility;

namespace GameRes.Formats.Fog {
    internal class DatEntry : Entry {
        public string FileName;
    }

    [Export(typeof(ArchiveFormat))]
    public class DatOpener : ArchiveFormat {
        public override string         Tag { get { return "DAT/FOG"; } }
        public override string Description { get { return "FOG resource archive"; } }
        public override uint     Signature { get { return 0; } }
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen(ArcView file) {
            var base_name = Path.GetFileNameWithoutExtension(file.Name);
            bool multipart = base_name.Contains("_");
            if (multipart)
                base_name = base_name.Split('_')[0];
            var index_file_name = base_name + "File.dat";
            if (!File.Exists(index_file_name))
                return null;

            var index = File.ReadAllBytes(index_file_name);
            var transformer = new NotTransform();
            transformer.TransformBlock(index, 0, index.Length, index, 0);

            using (var mem = new MemoryStream(index))
            using (var reader = new BinaryReader(mem)) {
                var dir = new List<Entry>();

                while (mem.Position < mem.Length) {
                    uint name_length = Binary.BigEndian(reader.ReadUInt32());
                    string name = Binary.GetCString(reader.ReadBytes((int)name_length), 0);
                    var entry = Create<DatEntry>(name);
                    if (multipart) {
                        uint part = Binary.BigEndian(reader.ReadUInt32());
                        entry.FileName = string.Format("{0}_{1:00}.dat", base_name, part);
                        if (!File.Exists(entry.FileName))
                            return null;
                    }
                    else {
                        entry.FileName = file.Name;
                    }
                    reader.ReadUInt32();
                    entry.Offset = Binary.BigEndian(reader.ReadUInt32());
                    reader.ReadUInt32();
                    entry.Size = Binary.BigEndian(reader.ReadUInt32());
                    dir.Add(entry);
                }

                return new ArcFile(file, this, dir);
            }
        }

        public override Stream OpenEntry(ArcFile arc, Entry entry) {
            var dent = entry as DatEntry;
            using (var data_file = new ArcView(dent.FileName)) {
                var input = data_file.CreateStream(dent.Offset, dent.Size);
                return new XoredStream(input, 0xFF);
            }
        }
    }
}
