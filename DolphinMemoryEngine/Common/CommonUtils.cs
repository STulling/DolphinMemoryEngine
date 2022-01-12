using System;
using System.Collections.Generic;
using System.Text;

namespace DolphinMemoryEngine.Common
{
    internal static class CommonUtils
    {
        public static UInt16 bSwap16(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }
        public static UInt32 bSwap32(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }
        public static UInt64 bSwap64(UInt64 value)  
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }

        public static UInt32 dolphinAddrToOffset(UInt32 addr, UInt32 mem2_offset)
        {
            addr &= 0x7FFFFFFF;
            if (addr >= 0x10000000)
            {
                // MEM2, calculate correct address from MEM2 offset
                addr -= 0x10000000;
                addr += mem2_offset;
            }
            return addr;
        }

        public static UInt32 offsetToDolphinAddr(UInt32 offset, UInt32 mem2_offset)
        {
            if (offset < 0 || offset >= 0x2000000)
  {
                // MEM2, calculate correct address from MEM2 offset
                offset += 0x10000000;
                offset -= mem2_offset;
            }
            return offset | 0x80000000;
        }
    }
}
