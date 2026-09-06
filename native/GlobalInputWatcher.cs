using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BunnyPet
{
    // Low-level keyboard hook so the bunny can notice typing even while
    // it has no window focus (it never steals focus or reads key values).
    internal sealed class GlobalInputWatcher : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int VK_MENU = 0x12;
        private const int VK_CONTROL = 0x11;
        private const int VK_SHIFT = 0x10;
        private const uint VK_U = 0x55;
        private const uint VK_C = 0x43;
        private const uint VK_F12 = 0x7B;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private readonly LowLevelKeyboardProc callback;
        private IntPtr hookId = IntPtr.Zero;

        public event Action KeyPressed;
        public event Action CheatKeyTriggered;

        public GlobalInputWatcher()
        {
            callback = HookCallback;
            try
            {
                using (var process = Process.GetCurrentProcess())
                using (var module = process.MainModule)
                {
                    hookId = SetWindowsHookEx(WH_KEYBOARD_LL, callback, GetModuleHandle(module.ModuleName), 0);
                }
            }
            catch (Exception)
            {
                hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var handler = KeyPressed;
                if (handler != null) handler();

                try
                {
                    var hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    bool isCtrl = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 || (GetKeyState(VK_CONTROL) & 0x8000) != 0;
                    bool isShift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0 || (GetKeyState(VK_SHIFT) & 0x8000) != 0;
                    bool isAlt = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 || (GetKeyState(VK_MENU) & 0x8000) != 0;

                    // 전역 치트키 조합: Ctrl + Alt + U / Ctrl + Shift + U / Ctrl + F12
                    bool isCheat = (isCtrl && (isAlt || isShift) && (hookStruct.vkCode == VK_U || hookStruct.vkCode == VK_C))
                                   || (isCtrl && hookStruct.vkCode == VK_F12);

                    if (isCheat)
                    {
                        CheatKeyTriggered?.Invoke();
                    }
                }
                catch { }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
