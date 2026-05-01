using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RBX_AntiAFK.Core;

class WindowInfo
{
    public IntPtr Handle { get; }
    public string Title { get; }
    public bool IsMinimized => IsIconic(Handle);
    public bool IsForeground => GetForegroundWindow() == Handle;
    public bool IsVisible => IsWindowVisible(Handle);
    public bool IsValidWindow => IsWindow(Handle);

    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;
    private const int SW_HIDE = 0;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOACTIVATE = 0x0010;

    private static readonly IntPtr HWND_NOTOPMOST = new(-2);
    private static readonly IntPtr HWND_TOP = new(0);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int LWA_ALPHA = 0x2;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    public WindowInfo(IntPtr handle, string title)
    {
        Handle = handle;
        Title = title;
    }

    public void Show() => ShowWindow(Handle, SW_SHOW);
    public void Hide() => ShowWindow(Handle, SW_HIDE);
    public void Restore() => ShowWindow(Handle, SW_RESTORE);
    public void Minimize() => ShowWindow(Handle, SW_MINIMIZE);
    public void Maximize() => ShowWindow(Handle, SW_MAXIMIZE);
    public void Activate() => SetForegroundWindow(Handle);

    public void SetTransparency(int transparency)
    {
        var exStyle = GetWindowLongPtr(Handle, GWL_EXSTYLE);
        SetWindowLongPtr(Handle, GWL_EXSTYLE, new IntPtr(exStyle.ToInt64() | WS_EX_LAYERED));
        SetLayeredWindowAttributes(Handle, 0, (byte)transparency, LWA_ALPHA);
    }

    public void SetNoTopMost() => SetWindowPos(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
    public void SetTop() => SetWindowPos(Handle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
}

class WinManager
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private class EnumWindowsData
    {
        public required HashSet<int> ProcessIds;
        public required List<WindowInfo> Windows;
        public required object Lock;
    }

    public static List<WindowInfo> GetWindowsByProcessName(string processName)
    {
        var windows = new List<WindowInfo>();
        var processIds = new HashSet<int>();

        var processes = Process.GetProcessesByName(processName);
        foreach (var p in processes)
        {
            processIds.Add(p.Id);
            p.Dispose();
        }

        var dataLock = new object();
        var data = new EnumWindowsData { ProcessIds = processIds, Windows = windows, Lock = dataLock };
        var gch = GCHandle.Alloc(data);

        try
        {
            EnumWindows(EnumWindowsCallback, (IntPtr)gch);
        }
        finally
        {
            gch.Free();
        }

        return windows;
    }

    private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        var gch = (GCHandle)lParam;
        var data = (EnumWindowsData)gch.Target!;

        GetWindowThreadProcessId(hWnd, out uint processId);

        if (data.ProcessIds.Contains((int)processId))
        {
            var sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();

            lock (data.Lock)
            {
                data.Windows.Add(new WindowInfo(hWnd, title));
            }
        }

        return true;
    }

    public static List<WindowInfo> GetVisibleRobloxWindows() =>
        GetWindowsByProcessName("RobloxPlayerBeta")
            .Where(w => w.IsVisible && w.Title == "Roblox")
            .ToList();

    public static List<WindowInfo> GetHiddenRobloxWindows() =>
        GetWindowsByProcessName("RobloxPlayerBeta")
            .Where(w => !w.IsVisible && w.Title == "Roblox")
            .ToList();

    public static List<WindowInfo> GetAllRobloxWindows() =>
        GetWindowsByProcessName("RobloxPlayerBeta")
            .Where(w => w.Title == "Roblox")
            .ToList();

    public static WindowInfo? GetActiveWindow()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return null;

        var sb = new StringBuilder(512);
        GetWindowText(hWnd, sb, sb.Capacity);
        return new WindowInfo(hWnd, sb.ToString());
    }
}
