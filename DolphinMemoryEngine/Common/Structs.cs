using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DolphinMemoryEngine.Common
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESSENTRY32
	{
        public uint dwSize;
        public uint cntUsage;
        public int th32ProcessID; // Hope = Cope
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szExeFile;
    };

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct SYSTEM_INFO
	{
		public Int32 dwOemId;
		public Int32 dwPageSize;
		public UInt32 lpMinimumApplicationAddress;
		public UInt32 lpMaximumApplicationAddress;
		public IntPtr dwActiveProcessorMask;
		public Int32 dwNumberOfProcessors;
		public Int32 dwProcessorType;
		public Int32 dwAllocationGranularity;
		public Int16 wProcessorLevel;
		public Int16 wProcessorRevision;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct MEMORY_BASIC_INFORMATION
	{
		public IntPtr BaseAddress;
		public IntPtr AllocationBase;
		public Int32 AllocationProtect;
		public UInt32 RegionSize;
		public Int32 State;
		public Int32 Protect;
		public Int32 Type;
	}

	[Flags]
	public enum TypeEnum : uint
	{
		MEM_IMAGE = 0x01000000,
		MEM_MAPPED = 0x00040000,
		MEM_PRIVATE = 0x00020000,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct LUID
	{
		public Int32 LowPart;
		public Int32 HighPart;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct TOKEN_PRIVILEGES
	{
		public Int32 PrivilegeCount;
		public LUID_AND_ATTRIBUTES Privileges;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct LUID_AND_ATTRIBUTES
	{
		public LUID Luid;
		public Int32 Attributes;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PSAPI_WORKING_SET_EX_INFORMATION
	{
		public IntPtr VirtualAddress;

		public PSAPI_WORKING_SET_EX_BLOCK VirtualAttributes;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PSAPI_WORKING_SET_EX_BLOCK
	{
		public ulong Flags;

		public ulong Invalid;
	}
}
