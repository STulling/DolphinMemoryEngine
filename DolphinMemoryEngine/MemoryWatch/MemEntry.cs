using DolphinMemoryEngine.Common;
using DolphinMemoryEngine.DolphinProcess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace DolphinMemoryEngine.MemoryWatch
{
    public abstract class MemEntry
    {
    }
    public class MemEntry<T>: MemEntry
    {
        string m_label;
        UInt32 m_consoleAddress;
        bool m_isUnsigned;
        bool m_boundToPointer = false;
        List<int> m_pointerOffsets;
        bool m_isValidPointer = false;
        byte[] m_memory;
        int m_size;

        public MemEntry(string label, UInt32 consoleAddress, bool isUnsigned, bool isBoundToPointer, List<int>offsets=null)
        {
            m_label = label;
            m_consoleAddress = consoleAddress;
            m_isUnsigned = isUnsigned;
            m_pointerOffsets = offsets;
            m_size = Marshal.SizeOf(typeof(T));
            m_boundToPointer = isBoundToPointer;
            m_memory = new byte[m_size];
        }

        public void setOffsets(List<int> offsets)
        {
            m_pointerOffsets = offsets;
        }

        public T getValue()
        {
            this.readMemoryFromRAM();
            using (MemoryStream ms = new MemoryStream(m_memory))
            {
                using (var br = new BinaryReader(ms))
                {
                    return br.Read<T>();
                }
            }
        }

        public string getLabel()
        {
            return m_label;
        }

        UInt32 getConsoleAddress()
        {
            return m_consoleAddress;
        }

        bool isBoundToPointer()
        {
            return m_boundToPointer;
        }

        int getPointerOffset(int index)
        {
            return m_pointerOffsets[index];
        }

        List<int> getPointerOffsets()
        {
            return m_pointerOffsets;
        }

        int getPointerLevel()
        {
            return m_pointerOffsets.Count;
        }

        public byte[] getMemory()
        {
            return m_memory;
        }

        void removeOffset()
        {
            m_pointerOffsets.RemoveAt(m_pointerOffsets.Count - 1);
        }

        void addOffset(int offset)
        {
            m_pointerOffsets.Add(offset);
        }

        UInt32 getAddressForPointerLevel(int level)
        {
            if (!m_boundToPointer && level > m_pointerOffsets.Count && level > 0)
                return 0;

            UInt32 address = m_consoleAddress;
            byte[] addressBuffer = new byte[sizeof(UInt32)];
            for (int i = 0; i < level; ++i)
            {
                if (DolphinAccessor.readFromRAM(CommonUtils.dolphinAddrToOffset(address, DolphinAccessor.getMEM1ToMEM2Distance()), addressBuffer, sizeof(UInt32), true))
                {
                    address = BitConverter.ToUInt32(addressBuffer, 0);
                    if (DolphinAccessor.isValidConsoleAddress(address))
                        address += (uint)m_pointerOffsets[i];
                    else
                        return 0;
                }
                else
                {
                    return 0;
                }
            }
            return address;
        }

        string getAddressStringForPointerLevel(int level)
        {
            UInt32 address = getAddressForPointerLevel(level);
            if (address == 0)
            {
                return "???";
            }
            else
            {
                return address.ToString("X");
            }
        }

        public MemOperationReturnCode readMemoryFromRAM()
        {
            UInt32 realConsoleAddress = m_consoleAddress;
            UInt32 MEM2Distance = DolphinAccessor.getMEM1ToMEM2Distance();
            if (m_boundToPointer)
            {
                byte[] realConsoleAddressBuffer = new byte[sizeof(UInt32)];
                foreach (int offset in m_pointerOffsets)
                {
                    if (DolphinAccessor.readFromRAM(
                            CommonUtils.dolphinAddrToOffset(realConsoleAddress, MEM2Distance),
                            realConsoleAddressBuffer, sizeof(UInt32), true))
                    {
                        realConsoleAddress = BitConverter.ToUInt32(realConsoleAddressBuffer, 0);
                        if (DolphinAccessor.isValidConsoleAddress(realConsoleAddress))
                        {
                            realConsoleAddress += (uint)offset;
                        }
                        else
                        {
                            m_isValidPointer = false;
                            return MemOperationReturnCode.invalidPointer;
                        }
                    }
                    else
                    {
                        return MemOperationReturnCode.operationFailed;
                    }
                }
                // Resolve sucessful
                m_isValidPointer = true;
            }
            if (DolphinAccessor.readFromRAM(
                    CommonUtils.dolphinAddrToOffset(realConsoleAddress, MEM2Distance), m_memory,
                    m_size, MemoryCommon.shouldBeBSwappedForType(typeof(T))))
                return MemOperationReturnCode.OK;
            return MemOperationReturnCode.operationFailed;
        }

        MemOperationReturnCode writeMemoryToRAM(byte[] memory, int size)
        {
            UInt32 realConsoleAddress = m_consoleAddress;
            UInt32 MEM2Distance = DolphinAccessor.getMEM1ToMEM2Distance();
            if (m_boundToPointer)
            {
                byte[] realConsoleAddressBuffer = new byte[sizeof(UInt32)];
                foreach (int offset in m_pointerOffsets)
                {
                    if (DolphinAccessor.readFromRAM(
                            CommonUtils.dolphinAddrToOffset(realConsoleAddress, MEM2Distance),
                            realConsoleAddressBuffer, sizeof(UInt32), true))
                    {
                        realConsoleAddress = BitConverter.ToUInt32(realConsoleAddressBuffer, 0);
                        if (DolphinAccessor.isValidConsoleAddress(realConsoleAddress))
                        {
                            realConsoleAddress += (uint)offset;
                        }
                        else
                        {
                            m_isValidPointer = false;
                            return MemOperationReturnCode.invalidPointer;
                        }
                    }
                    else
                    {
                        return MemOperationReturnCode.operationFailed;
                    }
                }
                // Resolve sucessful
                m_isValidPointer = true;
            }

            if (DolphinAccessor.writeToRAM(
                    CommonUtils.dolphinAddrToOffset(realConsoleAddress, MEM2Distance), memory, size,
                    MemoryCommon.shouldBeBSwappedForType(typeof(T))))
                return MemOperationReturnCode.OK;
            return MemOperationReturnCode.operationFailed;
        }

    }
}
