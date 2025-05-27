using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Threading;

namespace KeyboardDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Key, Button> keyButtonMap = new Dictionary<Key, Button>();
        private readonly HashSet<Key> pressedKeys = new HashSet<Key>();
        private readonly SolidColorBrush normalBrush = new SolidColorBrush(Color.FromRgb(45, 45, 48));
        private readonly SolidColorBrush pressedBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
        private readonly SolidColorBrush functionKeyBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
        private readonly DispatcherTimer resetTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            InitializeKeyMap();
            InitializeEvents();
            InitializeTimer();
            
            // 设置窗口可以接收键盘焦点
            this.Focusable = true;
            this.Focus();
        }

        private void InitializeKeyMap()
        {
            keyButtonMap.Clear();
            
            // 添加键映射
            var keyMappings = new Dictionary<Key, Button>
            {
                // 功能键
                { Key.F1, F1 }, { Key.F2, F2 }, { Key.F3, F3 }, { Key.F4, F4 },
                { Key.F5, F5 }, { Key.F6, F6 }, { Key.F7, F7 }, { Key.F8, F8 },
                { Key.F9, F9 }, { Key.F10, F10 }, { Key.F11, F11 }, { Key.F12, F12 },

                // 数字键行
                { Key.OemTilde, Tilde },
                { Key.D1, D1 }, { Key.D2, D2 }, { Key.D3, D3 }, { Key.D4, D4 }, { Key.D5, D5 },
                { Key.D6, D6 }, { Key.D7, D7 }, { Key.D8, D8 }, { Key.D9, D9 }, { Key.D0, D0 },
                { Key.OemMinus, Minus }, { Key.OemPlus, Plus }, { Key.Back, Backspace },

                // QWERTY行
                { Key.Tab, Tab },
                { Key.Q, Q }, { Key.W, W }, { Key.E, E }, { Key.R, R }, { Key.T, T },
                { Key.Y, Y }, { Key.U, U }, { Key.I, I }, { Key.O, O }, { Key.P, P },
                { Key.OemOpenBrackets, LeftBracket }, { Key.OemCloseBrackets, RightBracket },
                { Key.OemBackslash, Backslash },

                // ASDF行
                { Key.CapsLock, CapsLock },
                { Key.A, A }, { Key.S, S }, { Key.D, D }, { Key.F, F }, { Key.G, G },
                { Key.H, H }, { Key.J, J }, { Key.K, K }, { Key.L, L },
                { Key.OemSemicolon, Semicolon }, { Key.OemQuotes, Quote }, { Key.Enter, Enter },

                // ZXCV行
                { Key.LeftShift, LeftShift }, { Key.RightShift, RightShift },
                { Key.Z, Z }, { Key.X, X }, { Key.C, C }, { Key.V, V }, { Key.B, B },
                { Key.N, N }, { Key.M, M },
                { Key.OemComma, Comma }, { Key.OemPeriod, Period }, { Key.OemQuestion, Slash },

                // 底部控制键
                { Key.LeftCtrl, LeftCtrl }, { Key.RightCtrl, RightCtrl },
                { Key.LWin, LeftWin }, { Key.RWin, RightWin },
                { Key.LeftAlt, LeftAlt }, { Key.RightAlt, RightAlt },
                { Key.Space, Space }, { Key.Apps, Menu },

                // 中间功能键区域
                { Key.PrintScreen, PrintScreen }, { Key.Scroll, ScrollLock }, { Key.Pause, Pause },
                { Key.Insert, Insert }, { Key.Home, Home }, { Key.PageUp, PageUp },
                { Key.Delete, Delete }, { Key.End, End }, { Key.PageDown, PageDown },
                { Key.Up, Up }, { Key.Down, Down }, { Key.Left, Left }, { Key.Right, Right },

                // 小键盘
                { Key.NumLock, NumLock },
                { Key.Divide, NumDivide }, { Key.Multiply, NumMultiply }, { Key.Subtract, NumSubtract },
                { Key.NumPad7, NumPad7 }, { Key.NumPad8, NumPad8 }, { Key.NumPad9, NumPad9 }, { Key.Add, NumAdd },
                { Key.NumPad4, NumPad4 }, { Key.NumPad5, NumPad5 }, { Key.NumPad6, NumPad6 },
                { Key.NumPad1, NumPad1 }, { Key.NumPad2, NumPad2 }, { Key.NumPad3, NumPad3 },
                { Key.NumPad0, NumPad0 }, { Key.Decimal, NumDecimal }
            };
            
            // 将映射添加到主字典
            foreach (var kvp in keyMappings)
            {
                keyButtonMap[kvp.Key] = kvp.Value;
            }
        }

        private void InitializeEvents()
        {
            // 监听全局键盘事件
            this.KeyDown += MainWindow_KeyDown;
            this.KeyUp += MainWindow_KeyUp;
            
            // 确保窗口获取焦点时能接收键盘事件
            this.MouseDown += (s, e) => this.Focus();
        }

        private void InitializeTimer()
        {
            resetTimer.Interval = TimeSpan.FromMilliseconds(300);
            resetTimer.Tick += ResetTimer_Tick;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // 如果键已经被按下，忽略重复的KeyDown事件
            if (pressedKeys.Contains(e.Key))
                return;
                
            pressedKeys.Add(e.Key);
            HighlightKey(e.Key, true);
            UpdateStatusInfo(e.Key, true);
            
            // 停止重置计时器，因为有键被按下
            resetTimer.Stop();
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            // 移除已释放的键
            if (pressedKeys.Remove(e.Key))
            {
                HighlightKey(e.Key, false);
                
                // 如果没有其他键被按下，启动重置计时器
                if (pressedKeys.Count == 0)
                {
                    resetTimer.Start();
                }
            }
        }

        private void ResetTimer_Tick(object sender, EventArgs e)
        {
            resetTimer.Stop();
            
            // 只重置没有被按下的键
            foreach (var kvp in keyButtonMap)
            {
                if (!pressedKeys.Contains(kvp.Key))
                {
                    ResetKeyAppearance(kvp.Value);
                }
            }
            
            // 如果没有键被按下，重置状态文本
            if (pressedKeys.Count == 0)
            {
                StatusText.Text = "就绪 - 按任意键开始检测";
                KeyInfoText.Text = "";
            }
        }

        private void HighlightKey(Key key, bool isPressed)
        {
            if (keyButtonMap.TryGetValue(key, out Button button))
            {
                if (isPressed)
                {
                    button.Background = pressedBrush;
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                    button.BorderThickness = new Thickness(2);
                }
                else
                {
                    ResetKeyAppearance(button);
                }
            }
        }

        private void ResetKeyAppearance(Button button)
        {
            // 根据按键类型设置正常颜色
            if (button.Style.ToString().Contains("FunctionKeyStyle"))
            {
                button.Background = functionKeyBrush;
            }
            else
            {
                button.Background = normalBrush;
            }
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            button.BorderThickness = new Thickness(1);
        }

        private void ResetAllKeys()
        {
            foreach (var button in keyButtonMap.Values)
            {
                ResetKeyAppearance(button);
            }
        }

        private void UpdateStatusInfo(Key key, bool isPressed)
        {
            if (isPressed)
            {
                StatusText.Text = $"检测到按键: {GetKeyDisplayName(key)}";
                KeyInfoText.Text = $"键码: {key} | VK: {KeyInterop.VirtualKeyFromKey(key)}";
            }
        }

        private string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                Key.Space => "空格键",
                Key.Enter => "回车键",
                Key.Tab => "制表符键",
                Key.Back => "退格键",
                Key.CapsLock => "大写锁定键",
                Key.LeftShift or Key.RightShift => "Shift键",
                Key.LeftCtrl or Key.RightCtrl => "Ctrl键",
                Key.LeftAlt or Key.RightAlt => "Alt键",
                Key.LWin or Key.RWin => "Windows键",
                Key.Apps => "菜单键",
                Key.OemTilde => "波浪号键 (~`)",
                Key.OemMinus => "减号键 (-_)",
                Key.OemPlus => "等号键 (=+)",
                Key.OemOpenBrackets => "左中括号键 ([{)",
                Key.OemCloseBrackets => "右中括号键 (]})",
                Key.OemBackslash => "反斜杠键 (\\|)",
                Key.OemSemicolon => "分号键 (;:)",
                Key.OemQuotes => "引号键 ('\")",
                Key.OemComma => "逗号键 (,<)",
                Key.OemPeriod => "句号键 (.>)",
                Key.OemQuestion => "斜杠键 (/?)",
                Key.NumLock => "数字锁定键",
                Key.Divide => "小键盘除法键 (/)",
                Key.Multiply => "小键盘乘法键 (*)",
                Key.Subtract => "小键盘减法键 (-)",
                Key.Add => "小键盘加法键 (+)",
                Key.Decimal => "小键盘小数点键 (.)",
                Key.PrintScreen => "截屏键",
                Key.Scroll => "滚动锁定键",
                Key.Pause => "暂停键",
                Key.Insert => "插入键",
                Key.Delete => "删除键",
                Key.Home => "Home键",
                Key.End => "End键",
                Key.PageUp => "Page Up键",
                Key.PageDown => "Page Down键",
                Key.Up => "向上方向键",
                Key.Down => "向下方向键",
                Key.Left => "向左方向键",
                Key.Right => "向右方向键",
                _ when key >= Key.F1 && key <= Key.F12 => $"功能键 {key}",
                _ when key >= Key.D0 && key <= Key.D9 => $"数字键 {key.ToString().Substring(1)}",
                _ when key >= Key.NumPad0 && key <= Key.NumPad9 => $"小键盘数字键 {key.ToString().Substring(6)}",
                _ when key >= Key.A && key <= Key.Z => $"字母键 {key}",
                _ => key.ToString()
            };
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.Focus();
        }
    }
}