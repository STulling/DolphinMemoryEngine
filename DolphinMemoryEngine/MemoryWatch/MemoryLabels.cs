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

        public void LoadString(string jsonString)
        {
            List<Dictionary<string, object>> data = JArray.Parse(jsonString).ToObject<List<Dictionary<string, object>>>();
            foreach (Dictionary<string, object> item in data)
            {
                MemEntry entry = null;
                List<int> pointerOffsets = null;
                if (item.ContainsKey("pointerOffsets"))
                {
                    List<string> offsets = ((JArray)item["pointerOffsets"]).ToObject<List<string>>();
                    pointerOffsets = new List<int>();
                    foreach (string val in offsets)
                    {
                        pointerOffsets.Add(int.Parse(val, System.Globalization.NumberStyles.HexNumber));
                    }
                }
                int typeIndex = Convert.ToInt32(item["typeIndex"]);
                string label = (string)item["label"];
                uint address = uint.Parse((string)item["address"], System.Globalization.NumberStyles.HexNumber);
                bool unsigned = (bool)item["unsigned"];
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

                labels[label] = entry;
            }
        }

        public void LoadFile(string file)
        {
            string jsonString = File.ReadAllText(file);
            LoadString(jsonString);
        }
    }
}
