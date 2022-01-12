using DolphinMemoryEngine.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace DolphinMemoryEngine.DolphinProcess
{
    public static class DolphinAccessor
    {
        static IDolphinProcess m_instance = null;
        static DolphinStatus m_status = DolphinStatus.unHooked;
        static byte[] m_updatedRAMCache = null;

        public static void init()
        {
            m_instance = new WindowsDolphinProcess();
        }

        public static void hook()
        {
            init();
            if (!m_instance.findPID())
            {
                m_status = DolphinStatus.notRunning;
            }
            else if (!m_instance.obtainEmuRAMInformations())
            {
                m_status = DolphinStatus.noEmu;
            }
            else
            {
                m_status = DolphinStatus.hooked;
                updateRAMCache();
            }
        }

        public static void unHook()
        {
            m_instance = null;
            m_status = DolphinStatus.unHooked;
        }

        public static DolphinStatus getStatus()
        {
            return m_status;
        }

        public static bool readFromRAM(ulong offset, byte[] buffer, int size, bool withBSwap)
        {
            return m_instance.readFromRAM(offset, buffer, size, withBSwap);
        }

        public static bool writeToRAM(ulong offset, byte[] buffer, int size, bool withBSwap)
        {
            return m_instance.writeToRAM(offset, buffer, size, withBSwap);
        }

        public static int getPID()
        {
            return m_instance.getPID();
        }

        public static UInt64 getEmuRAMAddressStart()
        {
            return m_instance.getEmuRAMAddressStart();
        }

        public static bool isMEM2Present()
        {
            return m_instance.isMEM2Present();
        }

        public static UInt32 getMEM1ToMEM2Distance()
        {
            if (m_instance == null)
            {
                return 0;
            }
            return m_instance.getMEM1ToMEM2Distance();
        }

        public static bool isValidConsoleAddress(UInt32 address)
        {
            if (getStatus() != DolphinStatus.hooked)
                return false;

            bool isMem1Address = address >= Consts.MEM1_START && address < Consts.MEM1_END;
            if (isMEM2Present())
                return isMem1Address || (address >= Consts.MEM2_START && address < Consts.MEM2_END);
            return isMem1Address;
        }

        public static MemOperationReturnCode updateRAMCache()
        {
            m_updatedRAMCache = null;

            // MEM2, if enabled, is read right after MEM1 in the cache so both regions are contigous
            if (isMEM2Present())
            {
                byte[] mem2_buffer = new byte[Consts.MEM2_SIZE];
                // Read Wii extra RAM
                if (!readFromRAM(
                        CommonUtils.dolphinAddrToOffset(Consts.MEM2_START, getMEM1ToMEM2Distance()), mem2_buffer, (int)Consts.MEM2_SIZE, false))
                    return MemOperationReturnCode.operationFailed;
                m_updatedRAMCache = new byte[Consts.MEM1_SIZE + Consts.MEM2_SIZE];
                Buffer.BlockCopy(mem2_buffer, 0, m_updatedRAMCache, (int)Consts.MEM1_SIZE, (int)Consts.MEM2_SIZE);
            }
            else
            {
                m_updatedRAMCache = new byte[Consts.MEM1_SIZE];
            }

            // Read GameCube and Wii basic RAM
            if (!readFromRAM(0, m_updatedRAMCache, (int)Consts.MEM1_SIZE, false))
                return MemOperationReturnCode.operationFailed;
            return MemOperationReturnCode.OK;
        }

        public static void copyRawMemoryFromCache(byte[] dest, UInt32 consoleAddress, int byteCount)
        {
            if (isValidConsoleAddress(consoleAddress) &&
                isValidConsoleAddress((consoleAddress + (UInt32)(byteCount)) - 1))
            {
                UInt32 MEM2Distance = getMEM1ToMEM2Distance();
                UInt32 offset = CommonUtils.dolphinAddrToOffset(consoleAddress, MEM2Distance);
                UInt32 ramIndex = 0;
                if (offset >= Consts.MEM1_SIZE)
                    // Need to account for the distance between the end of MEM1 and the start of MEM2
                    ramIndex = offset - (MEM2Distance - Consts.MEM1_SIZE);
                else
                    ramIndex = offset;
                Array.Copy(m_updatedRAMCache, (int)ramIndex, dest, 0, (int)byteCount);
            }
        }
    }

    public enum DolphinStatus
    {
        hooked,
        notRunning,
        noEmu,
        unHooked
    }
}
