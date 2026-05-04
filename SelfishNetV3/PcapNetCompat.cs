using Microsoft.Win32.SafeHandles;
using System;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace PcapNet
{
    public sealed class packet_headers
    {
        public uint caplen;
        public uint len;
    }

    public sealed class CPcapNet : IDisposable
    {
        private const int PcapErrorBufferSize = 256;
        private bool disposed;

        static CPcapNet()
        {
            PcapNativeLibrary.EnsureRegistered();
        }

        public IntPtr nicHandle = IntPtr.Zero;

        public bool pcapnet_openLive(string deviceName, int snaplen, int promisc, int timeout, string errbuf)
        {
            if (nicHandle != IntPtr.Zero)
            {
                return true;
            }

            var nativeName = NormalizeDeviceName(deviceName);
            var errorBuffer = new StringBuilder(PcapErrorBufferSize);
            try
            {
                nicHandle = NativeMethods.pcap_open_live(nativeName, snaplen, promisc, timeout, errorBuffer);
                return nicHandle != IntPtr.Zero;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        public int pcapnet_setFilter(string filter, uint netmask)
        {
            if (nicHandle == IntPtr.Zero)
            {
                return -1;
            }

            var program = new bpf_program();
            var compileResult = NativeMethods.pcap_compile(nicHandle, ref program, filter, 1, netmask);
            if (compileResult != 0)
            {
                return compileResult;
            }

            try
            {
                return NativeMethods.pcap_setfilter(nicHandle, ref program);
            }
            finally
            {
                NativeMethods.pcap_freecode(ref program);
            }
        }

        public int pcapnet_next_ex(out packet_headers pkt_hdr, out byte[] pkt_data)
        {
            pkt_hdr = new packet_headers();
            pkt_data = Array.Empty<byte>();

            if (nicHandle == IntPtr.Zero)
            {
                return -1;
            }

            var result = NativeMethods.pcap_next_ex(nicHandle, out var headerPtr, out var dataPtr);
            if (result <= 0 || headerPtr == IntPtr.Zero || dataPtr == IntPtr.Zero)
            {
                return result;
            }

            var header = Marshal.PtrToStructure<pcap_pkthdr>(headerPtr);
            pkt_hdr.caplen = header.caplen;
            pkt_hdr.len = header.len;
            pkt_data = new byte[header.caplen];
            Marshal.Copy(dataPtr, pkt_data, 0, checked((int)header.caplen));
            return result;
        }

        public int pcapnet_sendpacket(byte[] packet)
        {
            if (nicHandle == IntPtr.Zero || packet == null || packet.Length == 0)
            {
                return -1;
            }

            return NativeMethods.pcap_sendpacket(nicHandle, packet, packet.Length);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (nicHandle != IntPtr.Zero)
                {
                    NativeMethods.pcap_close(nicHandle);
                    nicHandle = IntPtr.Zero;
                }

                disposed = true;
            }
        }

        private static string NormalizeDeviceName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return deviceName;
            }

            if (deviceName.StartsWith(@"\Device\NPF_", StringComparison.OrdinalIgnoreCase) ||
                deviceName.StartsWith(@"\\Device\\NPF_", StringComparison.OrdinalIgnoreCase) ||
                deviceName.StartsWith("rpcap://", StringComparison.OrdinalIgnoreCase))
            {
                return deviceName;
            }

            return deviceName.StartsWith("{", StringComparison.Ordinal) && deviceName.EndsWith("}", StringComparison.Ordinal)
                ? @"\Device\NPF_" + deviceName
                : deviceName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct timeval
        {
            public nint tv_sec;
            public nint tv_usec;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct pcap_pkthdr
        {
            public timeval ts;
            public uint caplen;
            public uint len;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct bpf_program
        {
            public uint bf_len;
            public IntPtr bf_insns;
        }

        private static class NativeMethods
        {
            [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern IntPtr pcap_open_live(string device, int snaplen, int promisc, int to_ms, StringBuilder errbuf);

            [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int pcap_next_ex(IntPtr p, out IntPtr pkt_header, out IntPtr pkt_data);

            [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int pcap_sendpacket(IntPtr p, byte[] buf, int size);

            [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern int pcap_compile(IntPtr p, ref bpf_program fp, string str, int optimize, uint netmask);

            [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int pcap_setfilter(IntPtr p, ref bpf_program fp);

            [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void pcap_freecode(ref bpf_program fp);

            [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void pcap_close(IntPtr p);
        }
    }

    public sealed unsafe class Driver
    {
        public bool create()
        {
            return IsDriverAvailable();
        }

        public IntPtr openDeviceDriver(sbyte* driverName)
        {
            var name = Marshal.PtrToStringAnsi((IntPtr)driverName) ?? "npf";
            return IsServiceInstalled(name) || TryOpenDevice(name) ? new IntPtr(1) : IntPtr.Zero;
        }

        private static bool IsDriverAvailable()
        {
            return IsServiceInstalled("npf") ||
                   IsServiceInstalled("npcap") ||
                   TryOpenDevice("npf") ||
                   TryOpenDevice("NPF") ||
                   TryOpenDevice("NPCAP") ||
                   PcapNativeLibrary.TryFindWpcap(out _);
        }

        private static bool IsServiceInstalled(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name);
            return key != null;
        }

        private static bool TryOpenDevice(string name)
        {
            using var handle = NativeMethods.CreateFile(
                @"\.\" + name,
                0,
                FileShare.ReadWrite,
                IntPtr.Zero,
                FileMode.Open,
                0,
                IntPtr.Zero);

            return !handle.IsInvalid;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFile(
                string fileName,
                uint desiredAccess,
                FileShare shareMode,
                IntPtr securityAttributes,
                FileMode creationDisposition,
                uint flagsAndAttributes,
                IntPtr templateFile);
        }
    }

    internal enum FileShare : uint
    {
        Read = 0x00000001,
        Write = 0x00000002,
        ReadWrite = Read | Write
    }

    internal enum FileMode : uint
    {
        Open = 3
    }

    internal static class PcapNativeLibrary
    {
        private static readonly object SyncRoot = new();
        private static bool registered;

        public static void EnsureRegistered()
        {
            if (registered)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (registered)
                {
                    return;
                }

                NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), ResolveNativeLibrary);
                registered = true;
            }
        }

        public static bool TryFindWpcap(out string path)
        {
            foreach (var candidate in GetWpcapCandidates())
            {
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            path = string.Empty;
            return false;
        }

        private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!libraryName.Equals("wpcap.dll", StringComparison.OrdinalIgnoreCase) &&
                !libraryName.Equals("wpcap", StringComparison.OrdinalIgnoreCase))
            {
                return IntPtr.Zero;
            }

            foreach (var candidate in GetWpcapCandidates())
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
                {
                    return handle;
                }
            }

            return IntPtr.Zero;
        }

        private static string[] GetWpcapCandidates()
        {
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var baseDirectory = AppContext.BaseDirectory;

            return RuntimeInformation.ProcessArchitecture == Architecture.X86
                ? new[]
                {
                    Path.Combine(baseDirectory, "wpcap.dll"),
                    Path.Combine(windows, "SysWOW64", "Npcap", "wpcap.dll"),
                    Path.Combine(windows, "SysWOW64", "wpcap.dll"),
                    Path.Combine(windows, "System32", "Npcap", "wpcap.dll"),
                    Path.Combine(windows, "System32", "wpcap.dll"),
                }
                : new[]
                {
                    Path.Combine(baseDirectory, "wpcap.dll"),
                    Path.Combine(windows, "System32", "Npcap", "wpcap.dll"),
                    Path.Combine(windows, "System32", "wpcap.dll"),
                    Path.Combine(windows, "Sysnative", "Npcap", "wpcap.dll"),
                    Path.Combine(windows, "Sysnative", "wpcap.dll"),
                    Path.Combine(windows, "SysWOW64", "Npcap", "wpcap.dll"),
                    Path.Combine(windows, "SysWOW64", "wpcap.dll"),
                };
        }
    }
}
