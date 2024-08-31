using System;
using System.Runtime.InteropServices;
using DataTool.Flag;

namespace DataTool.Helper;

public static class LaunchHelpers {
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

    [DllImport("user32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    const uint MB_OK = 0x0;
    const uint MB_ICONWARNING = 0x30;

    /// <summary>
    /// Verify that the application was launched from a console.
    /// </summary>
    public static void VerifyConsoleLaunch() {
        try {
            // If we're not on Windows or we have arguments, we're not going to show the message box.
            if (!OperatingSystem.IsWindows() || FlagParser.AppArgs.Length > 0) {
                return;
            }

            // Reference: https://devblogs.microsoft.com/oldnewthing/20160125-00/?p=92922
            var processList = new uint[2];
            var processCount = GetConsoleProcessList(processList, (uint) processList.Length);

            if (processCount != 1) {
                return;
            }

            _ = MessageBoxW(IntPtr.Zero, "DataTool is a console application, there is no GUI.\n\nYou need to use a terminal/console to run the tool.",
                            "DataTool", MB_OK | MB_ICONWARNING);
        } catch {
            // ignored
        }
    }
}