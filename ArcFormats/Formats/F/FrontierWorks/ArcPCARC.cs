using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Formats.FrontierWorks
{
    [Export(typeof(ArchiveFormat))]
    public class PcArcOpener : ArchiveFormat
    {
        public override string         Tag { get { return "PCARC"; } }
        public override string Description { get { return "Frontier Works engine resource archive"; } }
        public override uint     Signature { get { return 0x30303130; } } // "0100"
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public PcArcOpener()
        {
            Extensions = new string[] { "pcarc" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.View.AsciiEqual (0, "0100"))
                return null;
            var dir_count = file.View.ReadInt32 (4);
            if (dir_count <= 0 || dir_count > 32)
                return null;
            var data_offset = file.View.ReadInt64 (8);
            if (data_offset < 0x190)
                return null;
            var offset = 0x10;
            var dir = new List<Entry> ();
            for (var i = 0; i < dir_count; i++)
            {
                var dir_offset = file.View.ReadInt64 (offset);
                offset += 8;
                if (dir_offset < 0x190)
                    break;
                var path = file.View.ReadString (dir_offset, 0x40);
                var entry_count = file.View.ReadInt32 (dir_offset+0x40);
                dir_offset += 0x50;
                if (!string.IsNullOrEmpty (path) && !path.EndsWith ("/"))
                    path += "/";
                for (var j = 0; j < entry_count; j++)
                {
                    var entry = new Entry ();
                    entry.Name = path+file.View.ReadString (dir_offset, 0x40);
                    entry.Offset = data_offset+file.View.ReadInt64 (dir_offset+0x40);
                    entry.Size = file.View.ReadUInt32 (dir_offset+0x48);
                    dir.Add (entry);
                    dir_offset += 0x50;
                }
            }
            DetectFileTypes (dir);
            return new ArcFile (file, this, dir);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            if (entry.Name.HasExtension (".gz"))
            {
                using (var input = arc.File.CreateStream (entry.Offset, entry.Size))
                using (var gzs = new GZipStream (input, CompressionMode.Decompress))
                {
                    var output = new MemoryStream ();
                    gzs.CopyTo (output);
                    return new BinMemoryStream (output, entry.Name.Substring (0, entry.Name.Length-3));
                }
            }
            return base.OpenEntry (arc, entry);
        }

        static void DetectFileTypes (List<Entry> dir)
        {
            foreach (var entry in dir)
            {
                var name = entry.Name;
                if (name.HasExtension (".gz"))
                    name = name.Substring (0, name.Length-3);
                if (name.HasExtension (".oggl"))
                    entry.Type = "audio";
                if (string.IsNullOrEmpty (entry.Type))
                    entry.Type = FormatCatalog.Instance.GetTypeFromName (name);
            }
        }
    }
}
