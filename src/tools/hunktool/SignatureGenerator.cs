﻿using DocoptNet;
using Reko.Core;
using Reko.ImageLoaders.Hunk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace hunktool
{
    public class SignatureGenerator : Program.HunkCommand
    {
        public SignatureGenerator(IDictionary<string, ValueObject> args) : base(args)
        {
        }

        // Generate signatures to Output.
        public override bool handle_hunk_file(string s, HunkFile hunk_file)
        {
            if (hunk_file.units != null)
            {
                foreach (var unit in hunk_file.units)
                {
                    foreach (var segment in unit.segments)
                    {
                        var main = segment[0];
                        if (main.HunkType == HunkType.HUNK_CODE && main.Data != null &&
                            main.Data.Length != 0)
                        {
                            GenerateSignature(segment);
                        }
                    }
                }
            }

            return true;
        }

        private const int MaxSignatureLength = 32;

        private void GenerateSignature(List<Hunk> segment)
        {
            var main = segment[0];
            var extRefs =
                segment.OfType<ExtHunk>()
                .SelectMany(e => e.ext_ref)
                .Where(e => e.refs != null)
                .SelectMany(e => e.refs.Select(r => new { e.type, offset = r }))
                .ToDictionary(e => (int)e.offset, e => SizeOfRef(e.type));
            var defs =
                segment.OfType<ExtHunk>()
                .SelectMany(e => e.ext_def)
                .Where(e => e.type == ExtType.EXT_DEF)
                .GroupBy(e => e.def)
                .Select(g => new ExtObject { def = g.Key, name = g.Min(i => i.name) })
                .ToSortedList(e => (int)e.def, e => e.name);
            var ee = defs.GetEnumerator();
            if (!ee.MoveNext())
                return;
            var prev = ee.Current;
            while (ee.MoveNext())
            {
                var iStart = prev.Key;
                var iEnd = ee.Current.Key;
                WriteSignatureBytes(main, extRefs, iStart, iEnd, prev.Value);
                prev = ee.Current;
            }
            WriteSignatureBytes(main, extRefs, prev.Key, main.Data.Length, prev.Value);
        }

        private void WriteSignatureBytes(Hunk main, Dictionary<int, int> extRefs, int iStart, int iEnd, string name)
        {
            int i;
            int cbVariant = 0;
            iEnd = Math.Min(iStart + MaxSignatureLength, iEnd);
            for (i = iStart; i < iEnd; ++i)
            {
                if (cbVariant > 0 || extRefs.TryGetValue(i, out cbVariant))
                {
                    Output.Write("..");
                    --cbVariant;
                }
                else
                {
                    Output.Write("{0:X2}", (uint)main.Data[i]);
                }
            }
            var cPadding = (iStart + MaxSignatureLength) - iEnd;
            if (cPadding > 0)
                Output.Write(new string(' ', 2 * cPadding));
            Output.WriteLine(" {0}", name);
        }

        private int SizeOfRef(ExtType ext)
        {
            switch (ext)
            {
            case ExtType.EXT_RELREF16: return 2;
            case ExtType.EXT_DEF: return 4;
            case ExtType.EXT_ABSREF32: return 4;
            default: throw new NotImplementedException(string.Format("Unknown {0}", ext));
            }
        }
    }

}
