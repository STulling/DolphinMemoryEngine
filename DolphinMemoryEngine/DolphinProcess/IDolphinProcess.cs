using System;
using System.Collections.Generic;
using System.Text;

namespace DolphinMemoryEngine.DolphinProcess
{
    internal interface IDolphinProcess
    {
        bool findPID();
        bool obtainEmuRAMInformations();
        bool readFromRAM(ulong offset, byte[] buffer, int size, bool withBSwap);
        bool writeToRAM(ulong offset, byte[] buffer, int size, bool withBSwap);

        int getPID();
        UInt64 getEmuRAMAddressStart();
        bool isMEM2Present();
        UInt64 getMEM2AddressStart();
        UInt32 getMEM1ToMEM2Distance();
    }
}
