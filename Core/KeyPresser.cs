using System.Runtime.InteropServices;

namespace RBX_AntiAFK.Core;

public class KeyPresser
{
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private int _interactionDelay = 30;
    private int _keypressDelay = 45;

    public int InteractionDelay
    {
        get => _interactionDelay;
        set => _interactionDelay = value > 0 ? value : 30;
    }

    public int KeypressDelay
    {
        get => _keypressDelay;
        set => _keypressDelay = value > 0 ? value : 45;
    }

    public async Task PressKeyAsync(Keys key)
    {
        SendKeyDown(key);
        await Task.Delay(KeypressDelay);
        SendKeyUp(key);
    }

    public async Task PressSpaceAsync()
    {
        await PressKeyAsync(Keys.Space);
    }

    public async Task MoveCameraAsync()
    {
        await PressKeyAsync(Keys.I);
        await Task.Delay(InteractionDelay);
        await PressKeyAsync(Keys.O);
    }

    public void PressKey(Keys key)
    {
        SendKeyDown(key);
        Thread.Sleep(KeypressDelay);
        SendKeyUp(key);
    }

    public void PressSpace()
    {
        PressKey(Keys.Space);
    }

    public void MoveCamera()
    {
        PressKey(Keys.I);
        Thread.Sleep(InteractionDelay);
        PressKey(Keys.O);
    }

    private static void SendKeyDown(Keys key)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)key,
                    wScan = (ushort)MapVirtualKey((uint)key, 0),
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyUp(Keys key)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)key,
                    wScan = (ushort)MapVirtualKey((uint)key, 0),
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }
}
