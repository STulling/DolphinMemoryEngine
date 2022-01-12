using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using DolphinMemoryEngine.Common;
using Newtonsoft.Json.Linq;

namespace DolphinMemoryEngine.MemoryWatch
{
    public class MemoryLabels
    {
        private static MemoryLabels instance;

        public static MemoryLabels Instance
        {
            get
            {
                if (instance == null)
                    instance = new MemoryLabels();
                return instance;
            }
        }

        Dictionary<string, MemEntry> labels;

        private MemoryLabels()
        {
            labels = new Dictionary<string, MemEntry>();
        }

        public MemEntry<T> getEntry<T>(string name)
        {
            return (MemEntry<T>)labels[name];
        }

        public void addEntry(string label, MemEntry entry)
        {
            labels[label] = entry;
        }

        [System.Serializable]
        public class ListObj
        {
            public Entry[] data;
        }

        [System.Serializable]
        public class Entry
        {
            public string label;
            public string[] pointerOffsets;
            public int typeIndex;
            public string address;
            public bool unsigned;
        }

        public void LoadString(string jsonString)
        {
            ListObj listobj = JObject.Parse(jsonString).ToObject<ListObj>();
            foreach (Entry item in listobj.data)
            {
                MemEntry entry = null;
                List<int> pointerOffsets = null;
                if (item.pointerOffsets != null)
                {
                    pointerOffsets = new List<int>();
                    foreach (string val in item.pointerOffsets)
                    {
                        pointerOffsets.Add(int.Parse(val, System.Globalization.NumberStyles.HexNumber));
                    }
                }
                int typeIndex = item.typeIndex;
                string label = item.label;
                uint address = uint.Parse(item.address, System.Globalization.NumberStyles.HexNumber);
                bool unsigned = item.unsigned;
                if (typeIndex == (int)MemType.type_byte)
                    entry = new MemEntry<byte>(label, address, unsigned, pointerOffsets != null, pointerOffsets);
                if (typeIndex == (int)MemType.type_halfword)
                    entry = new MemEntry<short>(label, address, unsigned, pointerOffsets != null, pointerOffsets);
                if (typeIndex == (int)MemType.type_word)
                    entry = new MemEntry<int>(label, address, unsigned, pointerOffsets != null, pointerOffsets);
                if (typeIndex == (int)MemType.type_float)
                    entry = new MemEntry<float>(label, address, unsigned, pointerOffsets != null, pointerOffsets);
                if (typeIndex == (int)MemType.type_double)
                    entry = new MemEntry<double>(label, address, unsigned, pointerOffsets != null, pointerOffsets);

                addEntry(label, entry);
            }
        }

        public void LoadFile(string file)
        {
            string jsonString = File.ReadAllText(file);
            LoadString(jsonString);
        }
    }
}
