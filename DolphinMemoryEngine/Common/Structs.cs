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
		public uint th32ProcessID;
		public IntPtr th32DefaultHeapID;
		public uint th32ModuleID;
		public uint cntThreads;
		public uint th32ParentProcessID;
		public int pcPriClassBase;
		public uint dwFlags;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct SYSTEM_INFO
	{
		public ushort processorArchitecture;
		ushort reserved;
		public uint pageSize;
		public IntPtr minimumApplicationAddress;
		public IntPtr maximumApplicationAddress;
		public IntPtr activeProcessorMask;
		public uint numberOfProcessors;
		public uint processorType;
		public uint allocationGranularity;
		public ushort processorLevel;
		public ushort processorRevision;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MEMORY_BASIC_INFORMATION
	{
		public IntPtr BaseAddress;
		public IntPtr AllocationBase;
		public int AllocationProtect;
		public short __alignment1;
		public long RegionSize;
		public int State;
		public int Protect;
		public int Type;
		//public int __alignment2;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MEMORY_BASIC_INFORMATION64
	{
		public ulong BaseAddress;
		public ulong AllocationBase;
		public int AllocationProtect;
		public int __alignment1;
		public ulong RegionSize;
		public int State;
		public int Protect;
		public int Type;
		public int __alignment2;
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
