﻿using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core
{
    public class SCBlockCompare
    {
        private readonly List<string> AddedKeys = new List<string>();
        private readonly List<string> RemovedKeys = new List<string>();
        private readonly List<string> TypesChanged = new List<string>();
        private readonly List<string> ValueChanged = new List<string>();

        public SCBlockCompare(SAV8SWSH s1, SAV8SWSH s2)
        {
            var b1 = s1.Blocks.BlockInfo;
            var b2 = s2.Blocks.BlockInfo;
            var names = s1.Blocks.GetType().GetAllConstantsOfType<uint>();
            string GetKeyName(uint key) => names.TryGetValue(key, out var val) ? val : $"{key:X8}";

            var hs1 = new HashSet<uint>(b1.Select(z => z.Key));
            var hs2 = new HashSet<uint>(b2.Select(z => z.Key));

            var unique = new HashSet<uint>(hs1);
            unique.SymmetricExceptWith(hs2);
            foreach (var k in unique)
            {
                var name = GetKeyName(k);
                if (hs1.Contains(k))
                {
                    var b = s1.Blocks.GetBlock(k);
                    RemovedKeys.Add($"{name} - {b.Type}");
                }
                else
                {
                    var b = s2.Blocks.GetBlock(k);
                    AddedKeys.Add($"{name} - {b.Type}");
                }
            }

            hs1.IntersectWith(hs2);
            foreach (var k in hs1)
            {
                var x1 = s1.Blocks.GetBlock(k);
                var x2 = s2.Blocks.GetBlock(k);
                var name = GetKeyName(x1.Key);
                if (x1.Type != x2.Type)
                {
                    TypesChanged.Add($"{name} - {x1.Type} => {x2.Type}");
                    continue;
                }
                if (x1.Data.Length != x2.Data.Length)
                {
                    ValueChanged.Add($"{name} - Length: {x1.Data.Length} => {x2.Data.Length}");
                    continue;
                }

                if (x1.Data.Length == 0)
                    continue;

                if (x1.Type == SCTypeCode.Object || x1.Type == SCTypeCode.Array)
                {
                    if (!x1.Data.SequenceEqual(x2.Data))
                        ValueChanged.Add($"{name} - Bytes Changed");
                    continue;
                }

                var val1 = x1.GetValue();
                var val2 = x2.GetValue();
                if (Equals(val1, val2))
                    continue;
                if (val1 is ulong u1 && val2 is ulong u2)
                    ValueChanged.Add($"{name} - {u1:X8} => {u2:x8}");
                else
                    ValueChanged.Add($"{name} - {val1} => {val2}");
            }
        }

        public IReadOnlyList<string> Summary()
        {
            var result = new List<string>();
            AddIfPresent(result, AddedKeys, "Blocks Added:");
            AddIfPresent(result, RemovedKeys, "Blocks Removed:");
            AddIfPresent(result, TypesChanged, "BlockType Changed:");
            AddIfPresent(result, ValueChanged, "Value Changed:");

            return result;

            static void AddIfPresent(List<string> result, IList<string> list, string hdr)
            {
                if (list.Count == 0)
                    return;
                result.Add(hdr);
                result.AddRange(list);
                result.Add(string.Empty);
            }
        }
    }
}