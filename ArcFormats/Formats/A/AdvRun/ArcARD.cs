using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using GameRes.Utility;
using GameRes.Compression;

namespace GameRes.Formats.AdvRun 
{
    internal class ArdArchive : ArcFile {
        private string m_key;

        public string Key { get { return m_key; } }

        public ArdArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, string key)
            : base (arc, impl, dir)
        {
            m_key = key;
        }
    }

    [Export(typeof(ArchiveFormat))]
    public class ArdOpener : ArchiveFormat {
        public override string         Tag { get { return "ARD"; } }
        public override string Description { get { return "ADVRUN resource archive"; } }
        public override uint     Signature { get { return 0; } }
        public override bool  IsHierarchic { get { return false; } }
        public override bool      CanWrite { get { return false; } }

        public ArdOpener() {
            Extensions = new string[] { "ard" };
        }

        public override ArcFile TryOpen(ArcView file) {
            if (!file.View.AsciiEqual(4, "ARD0"))
                return null;

            var inf_name = VFS.ChangeFileName(file.Name, "Game.Inf");
            if (!VFS.FileExists(inf_name))
                return null;

            using (var inf = VFS.OpenView(inf_name)) {
                var key = FindKey(inf);
                if (key == null)
                    return null;

                int count = file.View.ReadInt32(8);
                if (!IsSaneCount(count))
                    return null;

                var dir = new List<Entry>(count);
                uint index_offset = 0x100;
                for (int i = 0; i < count; i++) {
                    var name = file.View.ReadString(index_offset + 0xc, 0x21, Encodings.cp932);
                    var entry = new PackedEntry {
                        Name = name,
                        Offset = file.View.ReadUInt32(index_offset),
                        Size = Math.Min(
                                file.View.ReadUInt32(index_offset + 4),
                                file.View.ReadUInt32(index_offset + 8)),
                    };

                    name = name.ToLower();
                    if (name.EndsWith(".snf")) entry.Type = "script";
                    else if (name.EndsWith(".piz")) entry.Type = "image";
                    else entry.Type = FormatCatalog.Instance.GetTypeFromName(name);

                    if (!entry.CheckPlacement(file.MaxOffset))
                        return null;
                    index_offset += 0x2d;
                    dir.Add(entry);
                }

                return new ArdArchive(file, this, dir, key);
            }
        }

        public override Stream OpenEntry(ArcFile arc, Entry entry) {
            var ard = arc as ArdArchive;
            var data = ard.File.View.ReadBytes (entry.Offset, entry.Size);
            if (entry.Name.ToLower().EndsWith(".snf")) {
                XorDecrypt(data, ard.Key);
                var input = new MemoryStream(data, 4, (int)(entry.Size - 4));
                return new ZLibStream(input, CompressionMode.Decompress);
            }
            return new BinMemoryStream(data);
        }

        string FindKey(ArcView file) {
            var data = file.View.ReadBytes(0, (uint)file.MaxOffset);
            XorDecrypt(data, "1#jk@oih%6");
            using (var input = new MemoryStream(data, 4, data.Length - 4))
            using (var unpacked = new ZLibStream(input, CompressionMode.Decompress)) 
            using (var output = new MemoryStream()) {
                unpacked.CopyTo(output);
                var conf = Binary.GetCString(output.ToArray(), 0);
                var match = Regex.Match(conf, "^@gcode\\(\"(.*).\"\\)", RegexOptions.Multiline);
                if (match.Success)
                    return match.Groups[1].Value;
                else
                    return null;
            }
        }

        void XorDecrypt(byte[] buffer, string key) {
            var k = Encodings.cp932.GetBytes(key);
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] ^= k[i % k.Length];
        }
    }
}
