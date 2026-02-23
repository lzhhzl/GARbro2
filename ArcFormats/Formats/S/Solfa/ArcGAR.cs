using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using GameRes.Utility;

namespace GameRes.Formats.Solfa
{
    [Export(typeof(ArchiveFormat))]
    public class GarOpener : ArchiveFormat
    {
        public override string         Tag { get { return "GAR/SAS5"; } }
        public override string Description { get { return "Solfa engine video archive"; } }
        public override uint     Signature { get { return 0x20524147; } } // 'GAR '
        public override bool  IsHierarchic { get { return false; } }
        public override bool      CanWrite { get { return false; } }

        public GarOpener ()
        {
            Extensions = new string[] { "gar" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            int version = file.View.ReadInt32 (4);
            if (version != 1)
                return null;

            uint index_offset = file.View.ReadUInt32 (8);
            int block_start = file.View.ReadInt32 (0x14);
            int count = file.View.ReadInt32 (index_offset) - 1;
            if (!IsSaneCount (count))
                return null;
            var GetEntryName = CreateEntryNameDelegate (file.Name);

            index_offset += 0x20;
            var dir = new List<Entry> ();
            for (int i = 0; i < count; ++i)
            {
                int block = file.View.ReadInt32 (index_offset);
                if (block == block_start)
                    continue;
                var entry = new Entry {
                    Name    = GetEntryName (i, block - block_start - 1),
                    Offset  = file.View.ReadUInt32 (index_offset + 4),
                    Size    = file.View.ReadUInt32 (index_offset + 12),
                };
                if (!entry.CheckPlacement (file.MaxOffset))
                    return null;
                if (block > block_start)
                {
                    entry.Type = "video";
                }
                dir.Add (entry);
                index_offset += 0x14;
            }
            return new ArcFile (file, this, dir);
        }

        internal Func<int, int, string> CreateEntryNameDelegate (string arc_name)
        {
            var index = Sec5Opener.LookupIndex (arc_name);
            string base_name = Path.GetFileNameWithoutExtension (arc_name);
            if (null == index)
                return (n, m) => GetDefaultName (base_name, n);
            else
                return (n, m) => {
                    Entry entry;
                    if (index.TryGetValue (m, out entry))
                        return entry.Name.Substring (1); // remove leading slash
                    return GetDefaultName (base_name, n);
                };
        }

        internal static string GetDefaultName (string base_name, int n)
        {
            return string.Format ("{0}#{1:D5}", base_name, n);
        }
    }
}
