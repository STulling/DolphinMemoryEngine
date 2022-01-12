using System;
using System.Collections.Generic;
using System.Text;

namespace DolphinMemoryEngine.Common
{
    public static class Consts
    {
        public const UInt32 MEM1_SIZE = 0x1800000;
        public const UInt32 MEM1_START = 0x80000000;
        public const UInt32 MEM1_END = 0x81800000;

        public const UInt32 MEM2_SIZE = 0x4000000;
        public const UInt32 MEM2_START = 0x90000000;
        public const UInt32 MEM2_END = 0x94000000;
    }

    public enum MemType
    {
        type_byte = 0,
        type_halfword,
        type_word,
        type_float,
        type_double,
        type_string,
        type_byteArray,
        type_num
    };

    public enum MemBase
    {
        base_decimal = 0,
        base_hexadecimal,
        base_octal,
        base_binary,
        base_none // Placeholder when the base doesn't matter (ie. string)
    };

    public enum MemOperationReturnCode
    {
        invalidInput,
        operationFailed,
        inputTooLong,
        invalidPointer,
        OK
    };
}
