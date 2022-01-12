using DolphinMemoryEngine.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DolphinMemoryEngine.DolphinProcess
{
    public class WindowsDolphinProcess// : IDolhinProcess
    {
        // Function Imports
        [DllImport("KERNEL32.dll")] //[DllImport("toolhelp.dll")]
        public static extern int CreateToolhelp32Snapshot(uint flags, uint processid);
        [DllImport("KERNEL32.DLL")] //[DllImport("toolhelp.dll")] 
        public static extern int CloseHandle(int handle);
        [DllImport("KERNEL32.DLL")] //[DllImport("toolhelp.dll")
        public static extern int Process32Next(int handle, ref PROCESSENTRY32 pe);
        [DllImport("KERNEL32.DLL")] //[DllImport("toolhelp.dll")
        public static extern int Process32First(int handle, ref PROCESSENTRY32 pe);
        [DllImport("KERNEL32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("KERNEL32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("Kernel32.dll")]
        static extern Int32 VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, ref MEMORY_BASIC_INFORMATION buffer, UInt32 dwLength);

        [DllImport("psapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryWorkingSetEx(IntPtr hProcess, [In, Out] PSAPI_WORKING_SET_EX_INFORMATION[] pv, int cb);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        int m_PID = -1;
        UInt64 m_emuRAMAddressStart = 0;
        UInt64 m_MEM2AddressStart = 0;
        bool m_MEM2Present = false;

        IntPtr m_hDolphin;

        public int getPID()
        {
            return (int)m_PID;
        }
        public UInt64 getEmuRAMAddressStart()
        {
            return m_emuRAMAddressStart;
        }
        public bool isMEM2Present()
        {
            return m_MEM2Present;
        }
        public UInt64 getMEM2AddressStart()
        {
            return m_MEM2AddressStart;
        }
        public UInt32 getMEM1ToMEM2Distance()
        {
            if (!m_MEM2Present)
                return 0;
            return (UInt32)m_MEM2AddressStart - (UInt32)m_emuRAMAddressStart;
        }

        public bool findPID()
        {
            PROCESSENTRY32 entry;
            entry.dwSize = 296; // sizeof(ProcessEntry) is being jank

            int snapshot = CreateToolhelp32Snapshot(0x00000002, 0);

            PROCESSENTRY32 pe32 = new PROCESSENTRY32();
            if (Process32First(snapshot, ref pe32) != 0)
            {
                do
                {
                    if (pe32.szExeFile == "Dolphin.exe" ||
                        pe32.szExeFile == "DolphinQt2.exe" ||
                        pe32.szExeFile == "DolphinWx.exe")
                    {
                        m_PID = pe32.th32ProcessID;
                        break;
                    }
                } while (Process32Next(snapshot, ref pe32) != 0);
            }

            CloseHandle(snapshot);
            if (m_PID == -1)
                // Here, Dolphin doesn't appear to be running on the system
                return false;

            // Get the handle if Dolphin is running since it's required on Windows to read or write into the
            // RAM of the process and to query the RAM mapping information
            m_hDolphin = OpenProcess(ProcessAccessFlags.QueryInformation |
                                     ProcessAccessFlags.VirtualMemoryOperation |
                                     ProcessAccessFlags.VirtualMemoryRead |
                                     ProcessAccessFlags.VirtualMemoryWrite,
                                     false, m_PID);
            return true;
        }

        public bool obtainEmuRAMInformations()
        {
            MEMORY_BASIC_INFORMATION info = new MEMORY_BASIC_INFORMATION();
            uint info_size = (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION));
            bool MEM1Found = false;
            for (UIntPtr p = UIntPtr.Zero;
                VirtualQueryEx(m_hDolphin, p, ref info, info_size) == info_size; UIntPtr.Add(p, (int)info.RegionSize))
            {
                // Check region size so that we know it's MEM2
                if (info.RegionSize == 0x4000000)
                {
                    UInt64 regionBaseAddress = 0;
                    //std::memcpy(&regionBaseAddress, &(info.BaseAddress), sizeof(IntPtr));
                    regionBaseAddress = (uint)info.BaseAddress.ToInt64();
                    if (MEM1Found && regionBaseAddress > m_emuRAMAddressStart + 0x10000000)
                    {
                        // In some cases MEM2 could actually be before MEM1. Once we find MEM1, ignore regions of
                        // this size that are too far away. There apparently are other non-MEM2 regions of size
                        // 0x4000000.
                        break;
                    }
                    // View the comment for MEM1.
                    PSAPI_WORKING_SET_EX_INFORMATION[] wsInfo = new PSAPI_WORKING_SET_EX_INFORMATION[1];
                    wsInfo[0].VirtualAddress = info.BaseAddress;
                    if (QueryWorkingSetEx(m_hDolphin, wsInfo, Marshal.SizeOf(typeof(PSAPI_WORKING_SET_EX_INFORMATION))))
                    {
                        if (wsInfo[0].VirtualAttributes.Invalid == 0)
                        {
                            m_MEM2AddressStart = regionBaseAddress;
                            //std::memcpy(&m_MEM2AddressStart, &(regionBaseAddress), sizeof(UInt64));
                            m_MEM2Present = true;
                        }
                    }
                }
                else if (!MEM1Found && info.RegionSize == 0x2000000 && info.Type == (int)TypeEnum.MEM_MAPPED)
                {
                    // Here, it's likely the right page, but it can happen that multiple pages with these criteria
                    // exists and have nothing to do with the emulated memory. Only the right page has valid
                    // working set information so an additional check is required that it is backed by physical
                    // memory.
                    PSAPI_WORKING_SET_EX_INFORMATION[] wsInfo = new PSAPI_WORKING_SET_EX_INFORMATION[1];
                    wsInfo[0].VirtualAddress = info.BaseAddress;
                    if (QueryWorkingSetEx(m_hDolphin, wsInfo, Marshal.SizeOf(typeof(PSAPI_WORKING_SET_EX_INFORMATION))))
                    {
                        if (wsInfo[0].VirtualAttributes.Invalid == 0)
                        {
                            m_emuRAMAddressStart = (UInt64)info.BaseAddress.ToInt64();
                            //std::memcpy(&m_emuRAMAddressStart, &(info.BaseAddress), sizeof(info.BaseAddress));
                            MEM1Found = true;
                        }
                    }
                }
                if (MEM1Found && m_MEM2Present)
                    break;
            }
            if (m_emuRAMAddressStart == 0)
            {
                // Here, Dolphin is running, but the emulation hasn't started
                return false;
            }
            return true;
        }
        /*
        bool readFromRAM(const u32 offset, char* buffer, const size_t size,
                                        const bool withBSwap)
{
  u64 RAMAddress = m_emuRAMAddressStart + offset;
        SIZE_T nread = 0;
        bool bResult = ReadProcessMemory(m_hDolphin, (void*)RAMAddress, buffer, size, &nread);
  if (bResult && nread == size)
  {
    if (withBSwap)
    {
      switch (size)
      {
      case 2:
      {
        u16 halfword = 0;
        std::memcpy(&halfword, buffer, sizeof(u16));
        halfword = Common::bSwap16(halfword);
        std::memcpy(buffer, &halfword, sizeof(u16));
        break;
      }
      case 4:
      {
        u32 word = 0;
    std::memcpy(&word, buffer, sizeof(u32));
        word = Common::bSwap32(word);
        std::memcpy(buffer, &word, sizeof(u32));
        break;
      }
      case 8:
      {
    u64 doubleword = 0;
    std::memcpy(&doubleword, buffer, sizeof(u64));
    doubleword = Common::bSwap64(doubleword);
    std::memcpy(buffer, &doubleword, sizeof(u64));
    break;
}
      }
    }
    return true;
  }
  return false;
}

bool writeToRAM(const u32 offset, const char* buffer, const size_t size,
                                       const bool withBSwap)
{
    u64 RAMAddress = m_emuRAMAddressStart + offset;
    SIZE_T nread = 0;
    char* bufferCopy = new char[size];
    std::memcpy(bufferCopy, buffer, size);
    if (withBSwap)
    {
        switch (size)
        {
            case 2:
                {
                    u16 halfword = 0;
                    std::memcpy(&halfword, bufferCopy, sizeof(u16));
                    halfword = Common::bSwap16(halfword);
                    std::memcpy(bufferCopy, &halfword, sizeof(u16));
                    break;
                }
            case 4:
                {
                    u32 word = 0;
                    std::memcpy(&word, bufferCopy, sizeof(u32));
                    word = Common::bSwap32(word);
                    std::memcpy(bufferCopy, &word, sizeof(u32));
                    break;
                }
            case 8:
                {
                    u64 doubleword = 0;
                    std::memcpy(&doubleword, bufferCopy, sizeof(u64));
                    doubleword = Common::bSwap64(doubleword);
                    std::memcpy(bufferCopy, &doubleword, sizeof(u64));
                    break;
                }
        }
    }

    bool bResult = WriteProcessMemory(m_hDolphin, (void*)RAMAddress, bufferCopy, size, &nread);
    delete[] bufferCopy;
    return (bResult && nread == size);
}
    }
        */
    }
}
