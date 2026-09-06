using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace DevGitAtom.Engine;

/// <summary>
/// A wrapper around Windows ConPTY (Pseudo Console) API.
/// </summary>
public class PseudoConsole : IDisposable
{
    // --- Win32 APIs ---
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput, SafeFileHandle hOutput, uint dwFlags, out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, ref IntPtr value, IntPtr cbSize, IntPtr previousValue, IntPtr returnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreateProcess(
        string? lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFOEX lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;

    // --- Class Members ---
    private IntPtr _hPC;
    private PROCESS_INFORMATION _pi;
    private SafeFileHandle _inputWriteSide;
    private SafeFileHandle _outputReadSide;
    private FileStream _inputStream;
    private FileStream _outputStream;
    public event EventHandler<string>? OutputReceived;
    private CancellationTokenSource _cts = new();

    public void Start(string command, string workingDirectory)
    {
        // 1. Create Pipes
        if (!CreatePipe(out var inputReadSide, out _inputWriteSide, IntPtr.Zero, 0)) throw new Exception("Failed to create input pipe");
        if (!CreatePipe(out _outputReadSide, out var outputWriteSide, IntPtr.Zero, 0)) throw new Exception("Failed to create output pipe");

        // 2. Create Pseudo Console
        COORD size = new COORD { X = 120, Y = 30 };
        int hr = CreatePseudoConsole(size, inputReadSide, outputWriteSide, 0, out _hPC);
        if (hr != 0) throw new Exception($"CreatePseudoConsole failed with error {hr}");

        // Cleanup the read/write ends the pseudo console now owns
        inputReadSide.Dispose();
        outputWriteSide.Dispose();

        // 3. Setup StartupInfoEx with Pseudo Console attribute
        IntPtr sizePtr = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref sizePtr);
        IntPtr attrList = Marshal.AllocHGlobal(sizePtr.ToInt32());
        InitializeProcThreadAttributeList(attrList, 1, 0, ref sizePtr);

        IntPtr hPC_ptr = Marshal.AllocHGlobal(IntPtr.Size);
        Marshal.WriteIntPtr(hPC_ptr, _hPC);

        UpdateProcThreadAttribute(attrList, 0, (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, ref _hPC, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero);

        STARTUPINFOEX siex = new STARTUPINFOEX();
        siex.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
        siex.lpAttributeList = attrList;

        // 4. Create Process
        bool success = CreateProcess(
            null,
            command,
            IntPtr.Zero,
            IntPtr.Zero,
            false,
            EXTENDED_STARTUPINFO_PRESENT,
            IntPtr.Zero,
            workingDirectory,
            ref siex,
            out _pi);

        DeleteProcThreadAttributeList(attrList);
        Marshal.FreeHGlobal(attrList);
        Marshal.FreeHGlobal(hPC_ptr);

        if (!success) throw new Exception($"CreateProcess failed, error: {Marshal.GetLastWin32Error()}");

        // 5. Wrap streams and start reading
        _inputStream = new FileStream(_inputWriteSide, FileAccess.Write);
        _outputStream = new FileStream(_outputReadSide, FileAccess.Read);

        Task.Run(() => ReadOutputLoop());
    }

    public void Write(string text)
    {
        if (_inputStream == null) return;
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        _inputStream.Write(bytes, 0, bytes.Length);
        _inputStream.Flush();
    }

    private void ReadOutputLoop()
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                int bytesRead = _outputStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OutputReceived?.Invoke(this, text);
            }
        }
        catch { /* Process probably exited or stream closed */ }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _inputStream?.Dispose();
        _outputStream?.Dispose();
        if (_pi.hProcess != IntPtr.Zero) CloseHandle(_pi.hProcess);
        if (_pi.hThread != IntPtr.Zero) CloseHandle(_pi.hThread);
        if (_hPC != IntPtr.Zero) ClosePseudoConsole(_hPC);
    }
}
