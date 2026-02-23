using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace GameRes.Formats.Edoire
{
    [Export(typeof(ArchiveFormat))]
    public class ArcOpener : ArchiveFormat
    {
        public override string         Tag { get { return "ARC"; } }
        public override string Description { get { return "Edoire's resource archive"; } }
        public override uint     Signature { get { return 0x43524140; } } // "@ARCH000"
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public ArcOpener ()
        {
            Extensions = new string[] { "arc" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.View.AsciiEqual (0, "@ARCH000"))
                return null;
            var index_offset = file.View.ReadInt64 (file.MaxOffset-8);
            if (index_offset <= 0 || index_offset >= file.MaxOffset-12)
                return null;
            var count = file.View.ReadInt32 (index_offset);
            if (!IsSaneCount (count))
                return null;
            index_offset += 4;
            var dir = new List<Entry> (count);
            for (var i = 0; i < count; i++)
            {
                var len = file.View.ReadByte (index_offset);
                index_offset += 1;
                var name = file.View.ReadString (index_offset, len, Encoding.UTF8);
                index_offset += len;
                var offset = file.View.ReadInt64 (index_offset);
                index_offset += 8;
                var size = file.View.ReadInt64 (index_offset);
                index_offset += 9;
                len = file.View.ReadByte (index_offset);
                index_offset += 1;
                var path = file.View.ReadString (index_offset, len, Encoding.UTF8);
                index_offset += len;
                if (path.StartsWith ("/"))
                    path = path.Substring (1);
                if (!string.IsNullOrEmpty (path) && !path.EndsWith ("/"))
                    path += "/";
                var entry = Create<Entry> (path+name);
                entry.Offset = offset;
                entry.Size = Convert.ToUInt32 (size);
                if (!entry.CheckPlacement (file.MaxOffset))
                    return null;
                dir.Add (entry);
            }
            return new ArcFile (file, this, dir);
        }
    }
}
