using System;
using System.Collections.Generic;
using System.Text;

namespace DolphinMemoryEngine.DolphinProcess
{
    internal interface IDolhinProcess
    {
        bool findPID();
        bool obtainEmuRAMInformations();
        //bool readFromRAM(UInt32 offset, char* buffer, size_t size, bool withBSwap);
        //bool writeToRAM(UInt32 offset, char* buffer, size_t size, bool withBSwap);

        int getPID();
        UInt64 getEmuRAMAddressStart();
        bool isMEM2Present();
        UInt64 getMEM2AddressStart();
        UInt32 getMEM1ToMEM2Distance();
    }
}
