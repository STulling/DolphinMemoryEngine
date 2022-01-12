using DolphinMemoryEngine.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DolphinMemoryEngine.DolphinProcess
{
    public class WindowsDolphinProcess : IDolphinProcess
    {
        #region Imports
        // Function Imports
        [DllImport("KERNEL32.dll")]
        public static extern int CreateToolhelp32Snapshot(uint flags, uint processid);
        [DllImport("KERNEL32.DLL")]
        public static extern int CloseHandle(int handle);
        [DllImport("KERNEL32.DLL")]
        public static extern int Process32Next(int handle, ref PROCESSENTRY32 pe);
        [DllImport("KERNEL32.DLL", SetLastError = true)]
        public static extern int Process32First(int handle, ref PROCESSENTRY32 pe);
        [DllImport("KERNEL32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
        [DllImport("KERNEL32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);
        [DllImport("psapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryWorkingSetEx(IntPtr hProcess, [In, Out] PSAPI_WORKING_SET_EX_INFORMATION[] pv, int cb);
        #endregion Imports

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
            pe32.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
            if (Process32First(snapshot, ref pe32) == 1)
            {
                do
                {
                    if (pe32.szExeFile == "Dolphin.exe" ||
                        pe32.szExeFile == "DolphinQt2.exe" ||
                        pe32.szExeFile == "DolphinWx.exe")
                    {
                        m_PID = (int)pe32.th32ProcessID;
                        break;
                    }
                } while (Process32Next(snapshot, ref pe32) == 1);
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
            MEMORY_BASIC_INFORMATION64 info = new MEMORY_BASIC_INFORMATION64();
            uint info_size = (uint)Marshal.SizeOf(info);

            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            IntPtr proc_min_address = sys_info.minimumApplicationAddress;
            IntPtr proc_max_address = sys_info.maximumApplicationAddress;

            bool MEM1Found = false;
            for (ulong p = 0;
                VirtualQueryEx(m_hDolphin, (IntPtr)p, out info, info_size) == info_size; p += info.RegionSize)
            {
                // Check region size so that we know it's MEM2
                if (info.RegionSize == 0x4000000)
                {
                    UInt64 regionBaseAddress = 0;
                    //std::memcpy(&regionBaseAddress, &(info.BaseAddress), sizeof(IntPtr));
                    regionBaseAddress = (uint)info.BaseAddress;
                    if (MEM1Found && regionBaseAddress > m_emuRAMAddressStart + 0x10000000)
                    {
                        // In some cases MEM2 could actually be before MEM1. Once we find MEM1, ignore regions of
                        // this size that are too far away. There apparently are other non-MEM2 regions of size
                        // 0x4000000.
                        break;
                    }
                    // View the comment for MEM1.
                    PSAPI_WORKING_SET_EX_INFORMATION[] wsInfo = new PSAPI_WORKING_SET_EX_INFORMATION[1];
                    wsInfo[0].VirtualAddress = (IntPtr)info.BaseAddress;
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
                    wsInfo[0].VirtualAddress = (IntPtr)info.BaseAddress;
                    if (QueryWorkingSetEx(m_hDolphin, wsInfo, Marshal.SizeOf(typeof(PSAPI_WORKING_SET_EX_INFORMATION))))
                    {
                        if (wsInfo[0].VirtualAttributes.Invalid == 0)
                        {
                            m_emuRAMAddressStart = info.BaseAddress;
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

        public bool readFromRAM(ulong offset, byte[] buffer, int size, bool withBSwap)
        {
            ulong RAMAddress = m_emuRAMAddressStart + offset;
            IntPtr nread;
            bool bResult = ReadProcessMemory(m_hDolphin, (IntPtr)RAMAddress, buffer, size, out nread);
            if (bResult && nread.ToInt64() == size)
            {
                if (withBSwap)
                {
                    Array.Reverse(buffer);
                }
                return true;
            }
            return false;
        }

        public bool writeToRAM(ulong offset, byte[] buffer, int size, bool withBSwap)
        {
            ulong RAMAddress = m_emuRAMAddressStart + offset;
            IntPtr nread;
            byte[] bufferCopy = new byte[size];
            buffer.CopyTo(bufferCopy, 0);
            if (withBSwap)
            {
                Array.Reverse(bufferCopy);
            }

            bool bResult = WriteProcessMemory(m_hDolphin, (IntPtr)RAMAddress, bufferCopy, size, out nread);
            return (bResult && nread.ToInt64() == size);
        }
    }
}
