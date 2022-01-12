using DolphinMemoryEngine;
using DolphinMemoryEngine.DolphinProcess;
using DolphinMemoryEngine.MemoryWatch;
using DolphinMemoryEngine.Common;
using System.Text;

DolphinAccessor.hook();
MemoryLabels labels = MemoryLabels.Instance;
labels.LoadFile("data.json");
MemEntry<float> damage = labels.getEntry<float>("/Damage/P1 Damage");
while (true)
{
    Console.WriteLine(damage.getValue());
}