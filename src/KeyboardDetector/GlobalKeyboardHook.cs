using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace KeyboardDetector
{
    public class GlobalKeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int HC_ACTION = 0;
        private const uint LLKHF_EXTENDED = 0x01;

        private LowLevelKeyboardProc _proc = HookCallback;
        private IntPtr _hookID = IntPtr.Zero;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<GlobalKeyPressEventArgs>? KeyDown;
        public event EventHandler<GlobalKeyPressEventArgs>? KeyUp;

        public GlobalKeyboardHook()
        {
            _hookID = SetHook(_proc);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL,
                    proc,
                    GetModuleHandle(curModule.ModuleName),
                    0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= HC_ACTION)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                
                // 读取键盘状态结构体以获取扩展键标志
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool isExtended = (hookStruct.flags & LLKHF_EXTENDED) != 0;

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    Instance?.OnKeyDown(new GlobalKeyPressEventArgs { Key = key, VirtualKeyCode = vkCode, IsExtended = isExtended });
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    Instance?.OnKeyUp(new GlobalKeyPressEventArgs { Key = key, VirtualKeyCode = vkCode, IsExtended = isExtended });
                }
            }

            return CallNextHookEx(Instance?._hookID ?? IntPtr.Zero, nCode, wParam, lParam);
        }

        private static GlobalKeyboardHook? Instance { get; set; }

        public void StartHook()
        {
            Instance = this;
        }

        public void StopHook()
        {
            Instance = null;
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        protected virtual void OnKeyDown(GlobalKeyPressEventArgs e)
        {
            KeyDown?.Invoke(this, e);
        }

        protected virtual void OnKeyUp(GlobalKeyPressEventArgs e)
        {
            KeyUp?.Invoke(this, e);
        }

        #region Windows API

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        public void Dispose()
        {
            StopHook();
        }
    }

    public class GlobalKeyPressEventArgs : EventArgs
    {
        public Key Key { get; set; }
        public int VirtualKeyCode { get; set; }
        public bool IsExtended { get; set; }
        
        /// <summary>
        /// 获取区分扩展键的唯一键标识符
        /// </summary>
        public string GetUniqueKeyId()
        {
            // 对于Enter键，区分主键盘Enter和小键盘Enter
            // 根据Windows API：主键盘Enter是非扩展键，小键盘Enter是扩展键
            if (Key == Key.Enter)
            {
                return IsExtended ? "NumPadEnter" : "MainEnter";
            }
            
            // 对于其他需要区分的键，可以在这里添加
            // 例如：Ctrl、Alt、Shift等
            if (Key == Key.LeftCtrl || Key == Key.RightCtrl)
            {
                return IsExtended ? "RightCtrl" : "LeftCtrl";
            }
            
            if (Key == Key.LeftAlt || Key == Key.RightAlt)
            {
                return IsExtended ? "RightAlt" : "LeftAlt";
            }
            
            // 默认返回键名
            return Key.ToString();
        }
    }
} 