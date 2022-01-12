using System;
using DolphinMemoryEngine.Common;
using DolphinMemoryEngine.DolphinProcess;

namespace DolphinMemoryEngine
{
    public static class DirectAPI
    {
        public static float ReadFloat(uint offset)
        {
            byte[] buffer = new byte[4];
            DolphinAccessor.readFromRAM(CommonUtils.dolphinAddrToOffset(offset, DolphinAccessor.getMEM1ToMEM2Distance()),
                buffer, 4, true);
            return BitConverter.ToSingle(buffer, 0);
        }
    }
}
