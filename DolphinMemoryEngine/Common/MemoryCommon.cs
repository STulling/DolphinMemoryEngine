using System;
using System.Collections.Generic;
using System.Text;

namespace DolphinMemoryEngine.Common
{
    public static class MemoryCommon
    {
        public static int getSizeForType(MemType type, int length)
        {
            switch (type)
            {
                case MemType.type_byte:
                    return sizeof(Byte);
                case MemType.type_halfword:
                    return sizeof(UInt16);
                case MemType.type_word:
                    return sizeof(UInt32);
                case MemType.type_float:
                    return sizeof(float);
                case MemType.type_double:
                    return sizeof(double);
                case MemType.type_string:
                    return length;
                case MemType.type_byteArray:
                    return length;
                default:
                    return 0;
            }
        }

        public static bool shouldBeBSwappedForType(Type type)
        {
            if (type == typeof(Byte)) return false;
            if (type == typeof(short)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(double)) return true;
            if (type == typeof(string)) return false;
            if (type == typeof(byte[])) return false;
            return false;
        }

        static int getNbrBytesAlignementForType(MemType type)
        {
            switch (type)
            {
                case MemType.type_byte:
                    return 1;
                case MemType.type_halfword:
                    return 2;
                case MemType.type_word:
                    return 4;
                case MemType.type_float:
                    return 4;
                case MemType.type_double:
                    return 4;
                case MemType.type_string:
                    return 1;
                case MemType.type_byteArray:
                    return 1;
                default:
                    return 1;
            }
        }
    }
}
