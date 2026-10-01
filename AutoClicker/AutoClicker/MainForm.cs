using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;
using Control = System.Windows.Forms.Control;
using ComboBox = System.Windows.Forms.ComboBox;
using Form = System.Windows.Forms.Form;
using Application = System.Windows.Forms.Application;
using FormBorderStyle = System.Windows.Forms.FormBorderStyle;
using FormStartPosition = System.Windows.Forms.FormStartPosition;
using FormWindowState = System.Windows.Forms.FormWindowState;
using ComboBoxStyle = System.Windows.Forms.ComboBoxStyle;
using Message = System.Windows.Forms.Message;

using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Icon = System.Drawing.Icon;

namespace AutoClicker;

public class MainForm : Form
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint nInputs,
        INPUT[] pInputs,
        int cbSize);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint ms);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint ms);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll")]
    private static extern bool SetThreadPriority(
        IntPtr hThread,
        int nPriority);

    private const int THREAD_PRIORITY_TIME_CRITICAL = 15;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
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

    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    private static readonly Color BgColor = Color.FromArgb(18, 18, 26);
    private static readonly Color CardColor = Color.FromArgb(30, 30, 42);
    private static readonly Color TextColor = Color.FromArgb(235, 235, 245);
    private static readonly Color MutedColor = Color.FromArgb(150, 150, 170);
    private static readonly Color AccentColor = Color.FromArgb(120, 140, 255);
    private static readonly Color DangerColor = Color.FromArgb(230, 70, 100);
    private static readonly Color TopBtnColor = Color.FromArgb(45, 45, 60);
    private static readonly Color TopBtnHover = Color.FromArgb(70, 70, 95);

    private RoundButton btnMinimize = null!;
    private RoundButton btnClose = null!;

    private ModernSlider sliderCps = null!;
    private Label lblCpsValue = null!;
    private Label lblStatus = null!;
    private Label lblHotkey = null!;

    private ComboBox cmbMouseButton = null!;
    private ComboBox cmbActivate = null!;

    private ModernCheckBox chkEnable = null!;

    private volatile int activateKey = 5;
    private volatile uint mouseDownFlag = MOUSEEVENTF_LEFTDOWN;
    private volatile uint mouseUpFlag = MOUSEEVENTF_LEFTUP;
    private volatile int currentCps = 30;
    private volatile bool running = true;

    public MainForm()
    {
        timeBeginPeriod(1);

        Text = "Auto Clicker";
        ClientSize = new Size(420, 560);
        BackColor = BgColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10f);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        DoubleBuffered = true;

        ShowInTaskbar = true;
        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            try { Icon = new Icon("app.ico"); }
            catch { }
        }

        BuildUI();

        var clickThread = new Thread(ClickLogic)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };
        clickThread.Start();

        var syncThread = new Thread(SettingsSync)
        {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        syncThread.Start();

        FormClosing += (_, _) =>
        {
            running = false;
            timeEndPeriod(1);
        };
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTCAPTION = 2;

        base.WndProc(ref m);

        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTCAPTION;
        }
    }

    private void BuildUI()
    {
        var lblTitle = new Label
        {
            Text = "Auto Clicker",
            Font = new Font("Segoe UI Black", 18f, FontStyle.Bold),
            ForeColor = TextColor,
            AutoSize = true,
            Location = new Point(28, 22),
            BackColor = Color.Transparent
        };

        var lblSub = new Label
        {
            Text = "by t.me/Metreon1k",
            Font = new Font("Segoe UI", 9f),
            ForeColor = AccentColor,
            AutoSize = true,
            Location = new Point(30, 58),
            BackColor = Color.Transparent
        };

        btnClose = new RoundButton
        {
            Text = "✕",
            Size = new Size(34, 34),
            Location = new Point(ClientSize.Width - 46, 20),
            CornerRadius = 8,
            NormalColor = TopBtnColor,
            HoverColor = TopBtnHover,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        btnClose.Click += (_, _) => Application.Exit();

        btnMinimize = new RoundButton
        {
            Text = "─",
            Size = new Size(34, 34),
            Location = new Point(ClientSize.Width - 86, 20),
            CornerRadius = 8,
            NormalColor = TopBtnColor,
            HoverColor = TopBtnHover,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        btnMinimize.Click += (_, _) =>
            WindowState = FormWindowState.Minimized;

        var card = new CardPanel
        {
            Location = new Point(24, 100),
            Size = new Size(ClientSize.Width - 48, 300),
            FillColor = CardColor
        };

        var lblCps = new Label
        {
            Text = "СКОРОСТЬ (CPS)",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = MutedColor,
            AutoSize = true,
            Location = new Point(22, 20),
            BackColor = Color.Transparent
        };

        lblCpsValue = new Label
        {
            Text = "30",
            Font = new Font("Segoe UI Black", 22f, FontStyle.Bold),
            ForeColor = AccentColor,
            AutoSize = true,
            Location = new Point(300, 6),
            BackColor = Color.Transparent
        };

        sliderCps = new ModernSlider
        {
            Minimum = 1,
            Maximum = 200,
            Value = 30,
            Location = new Point(20, 55),
            Width = card.Width - 40,
            AccentColor = AccentColor
        };

        sliderCps.ValueChanged += (_, _) =>
        {
            currentCps = sliderCps.Value;
            lblCpsValue.Text = currentCps.ToString();
        };

        var sep1 = new Panel
        {
            BackColor = Color.FromArgb(45, 45, 60),
            Location = new Point(20, 95),
            Size = new Size(card.Width - 40, 1)
        };

        var lblMouse = new Label
        {
            Text = "КНОПКА МЫШИ",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = MutedColor,
            AutoSize = true,
            Location = new Point(22, 110),
            BackColor = Color.Transparent
        };

        cmbMouseButton = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10f),
            Location = new Point(22, 135),
            Width = card.Width - 44,
            Height = 30,
            BackColor = Color.FromArgb(45, 45, 60),
            ForeColor = TextColor
        };
        cmbMouseButton.Items.AddRange(
            new object[]
            {
                "Левая (ЛКМ)",
                "Правая (ПКМ)"
            });
        cmbMouseButton.SelectedIndex = 0;

        var lblKey = new Label
        {
            Text = "КЛАВИША АКТИВАЦИИ",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = MutedColor,
            AutoSize = true,
            Location = new Point(22, 178),
            BackColor = Color.Transparent
        };

        cmbActivate = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10f),
            Location = new Point(22, 203),
            Width = card.Width - 44,
            Height = 30,
            BackColor = Color.FromArgb(45, 45, 60),
            ForeColor = TextColor
        };
        cmbActivate.Items.AddRange(
            new object[]
            {
                "XButton1",
                "XButton2",
                "Mouse3 (колесо)",
                "R Key",
                "F Key"
            });
        cmbActivate.SelectedIndex = 0;

        chkEnable = new ModernCheckBox
        {
            LabelText = "Кликер активен",
            Location = new Point(20, 250),
            Width = card.Width - 40,
            Height = 30,
            Checked = true,
            TextColor = TextColor
        };

        card.Controls.AddRange(
            new Control[]
            {
                lblCps,
                lblCpsValue,
                sliderCps,
                sep1,
                lblMouse,
                cmbMouseButton,
                lblKey,
                cmbActivate,
                chkEnable
            });

        lblStatus = new Label
        {
            Text = "● ОЖИДАНИЕ",
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = MutedColor,
            AutoSize = true,
            Location = new Point(30, 425),
            BackColor = Color.Transparent
        };

        lblHotkey = new Label
        {
            Text = "Зажми клавишу — клики пойдут",
            Font = new Font("Segoe UI", 9f),
            ForeColor = MutedColor,
            AutoSize = true,
            Location = new Point(30, 452),
            BackColor = Color.Transparent
        };

        var btnExit = new RoundButton
        {
            Text = "ЗАКРЫТЬ",
            Size = new Size(ClientSize.Width - 48, 42),
            Location = new Point(24, ClientSize.Height - 58),
            CornerRadius = 12,
            NormalColor = DangerColor,
            HoverColor = Color.FromArgb(255, 90, 120)
        };
        btnExit.Click += (_, _) => Application.Exit();

        Controls.AddRange(
            new Control[]
            {
                lblTitle,
                lblSub,
                btnClose,
                btnMinimize,
                card,
                lblStatus,
                lblHotkey,
                btnExit
            });
    }

    private void SettingsSync()
    {
        while (running)
        {
            try
            {
                if (IsDisposed)
                    break;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(UpdateSettings));
                }
                else
                {
                    UpdateSettings();
                }
            }
            catch
            {
            }

            Thread.Sleep(100);
        }
    }

    private void UpdateSettings()
    {
        switch (cmbMouseButton.SelectedIndex)
        {
            case 0:
                mouseDownFlag = MOUSEEVENTF_LEFTDOWN;
                mouseUpFlag = MOUSEEVENTF_LEFTUP;
                break;

            case 1:
                mouseDownFlag = MOUSEEVENTF_RIGHTDOWN;
                mouseUpFlag = MOUSEEVENTF_RIGHTUP;
                break;
        }

        switch (cmbActivate.SelectedIndex)
        {
            case 0: activateKey = 5; break;
            case 1: activateKey = 6; break;
            case 2: activateKey = 4; break;
            case 3: activateKey = 82; break;
            case 4: activateKey = 70; break;
        }
    }

    private void ClickLogic()
    {
        try
        {
            SetThreadPriority(
                GetCurrentThread(),
                THREAD_PRIORITY_TIME_CRITICAL);
        }
        catch
        {
        }

        var inputs = new INPUT[2];
        inputs[0].type = INPUT_MOUSE;
        inputs[1].type = INPUT_MOUSE;

        int inputSize = Marshal.SizeOf<INPUT>();
        long frequency = Stopwatch.Frequency;
        long nextClick = 0;
        bool wasActive = false;

        long cpsStart = Stopwatch.GetTimestamp();
        int cpsCounter = 0;
        int displayedCps = 0;

        while (running)
        {
            bool enabled;
            try { enabled = chkEnable.Checked; }
            catch { enabled = false; }

            bool keyDown =
                (GetAsyncKeyState(activateKey) & 0x8000) != 0;

            bool active = enabled && keyDown;

            if (!active)
            {
                if (wasActive)
                {
                    wasActive = false;
                    cpsCounter = 0;
                    displayedCps = 0;
                    UpdateStatus(false, 0);
                }

                Thread.Sleep(2);
                continue;
            }

            if (!wasActive)
            {
                wasActive = true;
                cpsCounter = 0;
                displayedCps = 0;
                cpsStart = Stopwatch.GetTimestamp();
                nextClick = Stopwatch.GetTimestamp();
                UpdateStatus(true, 0);
            }

            int cps = currentCps;
            if (cps < 1) cps = 1;
            if (cps > 200) cps = 200;

            long interval = frequency / cps;
            long now = Stopwatch.GetTimestamp();

            if (now < nextClick)
            {
                WaitBusy(nextClick, frequency);
                continue;
            }

            inputs[0].mi.dwFlags = mouseDownFlag;
            inputs[1].mi.dwFlags = mouseUpFlag;

            SendInput(2, inputs, inputSize);

            cpsCounter++;
            nextClick += interval;

            now = Stopwatch.GetTimestamp();

            if (nextClick <= now)
                nextClick = now + interval;

            long elapsed = now - cpsStart;

            if (elapsed >= frequency)
            {
                displayedCps = (int)Math.Round(
                    cpsCounter * (double)frequency / elapsed);

                cpsCounter = 0;
                cpsStart = now;

                UpdateStatus(true, displayedCps);
            }
        }
    }

    private static void WaitBusy(long target, long frequency)
    {
        while (true)
        {
            long now = Stopwatch.GetTimestamp();
            long remaining = target - now;

            if (remaining <= 0)
                return;

            if (remaining > frequency / 1000)
                Thread.SpinWait(100);
            else
                Thread.SpinWait(10);
        }
    }

    private void UpdateStatus(bool active, int cps)
    {
        try
        {
            if (IsDisposed || Disposing)
                return;

            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || Disposing)
                    return;

                lblStatus.Text = active
                    ? $"● КЛИКАЮ  ({cps} CPS)"
                    : "● ОЖИДАНИЕ";

                lblStatus.ForeColor =
                    active ? AccentColor : MutedColor;
            }));
        }
        catch
        {
        }
    }
}