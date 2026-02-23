using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using GameRes.Compression;
using GameRes.Utility;

namespace GameRes.Formats.Anchor {
    internal class FpdArchive : ArcFile {
        public readonly byte[] Key;

        public FpdArchive(ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, byte[] key) : base (arc, impl, dir) {
            Key = key;
        }
    }

    [Export(typeof(ArchiveFormat))]
    public class FpdOpener : ArchiveFormat {
        public override string         Tag { get { return "FPD"; } }
        public override string Description { get { return "AGES Mk2 resource archive"; } }
        public override uint     Signature { get { return 0x00445046; } } // 'FPD\x00'
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen(ArcView file) {
            int version = Binary.BigEndian(file.View.ReadInt32(4));
            if (version != 2)
                return null;

            int count = (int)Binary.BigEndian(file.View.ReadInt64(8));
            if (!IsSaneCount(count))
                return null;

            long data_start = Binary.BigEndian(file.View.ReadInt64(0x10));
            long offset = 0x38;
            if (data_start < count * 32 + offset)
                return null;
            uint index_size = (uint)(data_start - offset);

            var key = QueryKey();
            var dir = new List<Entry>(count);

            using (var input = file.CreateStream(offset, (uint)(data_start - offset)))
            using (var decrypted = new ByteStringEncryptedStream(input, key))
            using (var reader = new BinaryReader(decrypted)) {
                var name_offsets = new List<long>(count);
                for (int i = 0; i < count; i++) {
                    long name_offset = Binary.BigEndian(reader.ReadInt64());
                    long data_offset = Binary.BigEndian(reader.ReadInt64());
                    long size = Binary.BigEndian(reader.ReadInt64());
                    long unpacked_size = Binary.BigEndian(reader.ReadInt64());

                    var entry = new PackedEntry {
                        Offset = data_start + data_offset,
                        Size = (uint)size,
                        UnpackedSize = (uint)unpacked_size,
                        IsPacked = unpacked_size != 0
                    };
                    if (!entry.CheckPlacement(file.MaxOffset))
                        return null;
                    dir.Add(entry);
                    name_offsets.Add(name_offset);
                }
                var name_block = reader.ReadBytes((int)(data_start - offset - count * 32));
                using (var mem = new MemoryStream(name_block))
                using (var stream = new ZLibStream(mem, CompressionMode.Decompress))
                using (var output = new MemoryStream()) {
                    stream.CopyTo(output);
                    var names = output.ToArray();
                    for (int i = 0; i < count; i++) {
                        int maxLength = names.Length - (int)name_offsets[i];
                        dir[i].Name = Binary.GetCString (names, (int)name_offsets[i], maxLength, Encoding.UTF8);
                        dir[i].Type = FormatCatalog.Instance.GetTypeFromName(dir[i].Name);
                    }
                }
                return new FpdArchive(file, this, dir, key);
            }
        }

        public override Stream OpenEntry(ArcFile arc, Entry entry) {
            var farc = arc as FpdArchive;
            var pent = entry as PackedEntry;
            var input = farc.File.CreateStream(entry.Offset, entry.Size);
            var decrypted = new ByteStringEncryptedStream(input, farc.Key);
            if (pent.IsPacked)
                return new ZLibStream(decrypted, CompressionMode.Decompress);
            else
                return decrypted;
            // TODO: epk decryption
        }

        byte[] QueryKey() {
            return DefaultScheme.ArchiveKey;
        }

        FpdScheme DefaultScheme = new FpdScheme();

        public override ResourceScheme Scheme {
            get { return DefaultScheme; }
            set { DefaultScheme = (FpdScheme)value; }
        }
    }

    [Serializable]
    public class FpdScheme : ResourceScheme {
        public byte[] ArchiveKey;
        public byte[] EpkKey;
    }
}
