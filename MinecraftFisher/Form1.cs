using GDIForm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftFisher
{
    public partial class Form1 : GDIForm.GDIForm
    {
        public Form1()
        {
            InitializeComponent();
            Resolution = new Size(320, 220);

        }

        const string desc = "Minecraft自动钓鱼机\r\n全版本通用，可开光影和粒子\r\n建议窗口化游戏，用默认窗口大小\r\n按Ctrl+Alt+H开始/停止";

        private void Form1_Load(object sender, EventArgs e)
        {
            Icon = Properties.Resources.favicon;
        }

        public override void beforeInit()
        {

        }

        public override void onInit(Graphics g)
        {
            ShowFPS = true;
            TopMost = true;
            captureArea = new Bitmap(Resolution.Width, Resolution.Height);

            Bitmap overlapTexture = new Bitmap(96, 64);
            using (Graphics gp = Graphics.FromImage(overlapTexture)) {
                gp.Clear(Color.FromArgb(225,Color.Black));
                gp.DrawString("禁止套娃", label1.Font, new SolidBrush(Color.FromArgb(64, Color.White)), new RectangleF(0, 0, 96, 64), centerstr);
            }
            overlapBrush = new TextureBrush(overlapTexture, System.Drawing.Drawing2D.WrapMode.Tile);

            setState(desc);
        }

        Bitmap captureArea = null;

        Brush overlapBrush = new SolidBrush(Color.Black);

        public void capture() {
            using (Graphics gpc = Graphics.FromImage(captureArea)) {
                gpc.Clear(Color.Transparent);
                Point beginPoint = new Point(MousePosition.X - Resolution.Width / 2, MousePosition.Y - Resolution.Height / 2);
                Point destPoint = label1.PointToScreen(Point.Empty);
                gpc.CopyFromScreen(beginPoint, Point.Empty, Resolution);
                gpc.FillRectangle(overlapBrush, destPoint.X - beginPoint.X, destPoint.Y - beginPoint.Y, Resolution.Width, Resolution.Height);
            }
        }

        bool inFishing = false;
        int stage = 0;
        Rectangle foundArea = Rectangle.Empty;

        public const int STAGE_THROW = 0;
        public const int STAGE_THROW_WAIT = 1;
        public const int STAGE_FIND_HOOK = 2;
        public const int STAGE_MONITOR_HOOK = 3;
        public const int STAGE_RELEASE_HOOK = 4;


        Pen green = new Pen(Brushes.Lime, 3);
        Pen red = new Pen(Brushes.Red, 3);

        public long commonStoptime = 0;

        private DateTime epoch = new DateTime(2020, 9, 15);
        public long SysClock {
            get {
                return (long)(DateTime.Now - epoch).TotalMilliseconds;
            }
        }

        public override void onDraw(Graphics g)
        {

            g.Clear(Color.Black);
            capture();

            g.DrawImage(captureArea, 0, 0);
            if (inFishing)
            {
                switch (stage)
                {
                    case STAGE_THROW:
                        leftDown();
                        stage++;
                        setTimeOut(Program.THROW_DELAY);
                        setState("正在锁定鱼钩");
                        break;

                    case STAGE_THROW_WAIT:

                        try
                        {
                            foundArea = findHooks(captureArea);

                            g.DrawRectangle(red, foundArea);
                        }
                        catch
                        {
                        }

                        if (isTimeout()) {
                            stage++;
                        }
                        break;
                    case STAGE_FIND_HOOK:
                        try
                        {
                            foundArea = findHooks(captureArea);
                            setState("等待上钩");
                            stage++;
                        }
                        catch  {
                            inFishing = false;
                            setState("[已停止] 鱼钩呢？");
                        }
                        break;

                    case STAGE_MONITOR_HOOK:
                        if (hasHook(captureArea, foundArea))
                        {
                            setTimeOut(Program.RELEASE_DELAY);

                            setState("等待上钩");
                            g.DrawRectangle(green, foundArea);
                        }
                        else {
                            g.DrawRectangle(red, foundArea);

                            setState("要来了，要来了");
                            if (isTimeout()) {
                                leftDown();
                                setTimeOut(500);
                                stage++;
                                setState("OHHHHHHHHHHHHHHHHHH");
                            }
                        }
                        break;

                    case STAGE_RELEASE_HOOK:
                        if (isTimeout())
                        {
                            stage = 0;
                        }
                        break;
                }
            }
            else {
                stage = 0;

                try
                {
                    foundArea = findHooks(captureArea);
                    g.DrawRectangle(green, foundArea);
                }
                catch
                {
                }
            }

            bool mhotKeyIsDown = keyIsDown(Keys.LControlKey) && keyIsDown(Keys.LMenu) && keyIsDown(Keys.H);


            if (mhotKeyIsDown && (!hotKeyIsDown)) {
                inFishing = !inFishing;
                if (!inFishing) {
                    setState(desc);
                }

            }

            g.DrawString(rawTitle, label1.Font, titlestr, this.ClientRectangle, bottomLeft);
            drawButton(g);
            hotKeyIsDown = mhotKeyIsDown;
        }

        void setTimeOut(long ms) { commonStoptime = SysClock + ms; }
        bool isTimeout() => commonStoptime < SysClock;
        void drawButton(Graphics g) {
            Rectangle rect = new Rectangle(button1.Left, button1.Top, button1.Width, button1.Height);
            g.FillRectangle(button1.Capture ? buttonFillMouseOver : buttonFill, rect);
            g.DrawRectangle(butonBorder, rect);
            g.DrawString(button1.Text, button1.Font, buttonstr, rect, centerstr);
        }

        Brush titlestr = new SolidBrush(Color.Orange);
        Brush buttonstr = new SolidBrush(Color.Lime);
        Pen butonBorder = new Pen(Color.Lime);
        Brush buttonFill = new SolidBrush(Color.FromArgb(96, Color.Black));
        Brush buttonFillMouseOver = new SolidBrush(Color.FromArgb(144, Color.Black));

        StringFormat bottomLeft = new StringFormat() { LineAlignment = StringAlignment.Far, Alignment = StringAlignment.Near };
        StringFormat centerstr= new StringFormat() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center ,FormatFlags = StringFormatFlags.FitBlackBox};

        bool hotKeyIsDown = false;

        String rawTitle="";
        void setState(String msg) {
            rawTitle = msg;
        }

        private Rectangle findHooks(Bitmap captureArea)
        {
            FastBitmap fbm = new FastBitmap(captureArea);
            fbm.LockBits();
            for (int x = 0; x < fbm.Width; x++)
            {
                for (int y = 0; y < fbm.Height; y++)
                {
                    if (isRed(fbm.GetPixel(x, y))) {
                        fbm.UnlockBits();
                        return new Rectangle(x - 10, y - 10, 24, 24);
                    }
                }
            }
            fbm.UnlockBits();
            throw new InstanceNotFoundException();
        }

        private bool isRed(Color c) {
            return Math.Abs(c.G - c.B) < 16 && c.R - c.G > 64;
        }

        private bool hasHook(Bitmap bmp, Rectangle range) {
            FastBitmap fbm = new FastBitmap(captureArea);
            fbm.LockBits();
            for (int x = Math.Max(0,range.Width); x < Math.Min(fbm.Width,range.X+range.Width); x++)
            {
                for (int y = Math.Max(0, range.Height); y < Math.Min(fbm.Height, range.Y+range.Height); y++)
                {
                    if (isRed(fbm.GetPixel(x, y)))
                    {
                        fbm.UnlockBits();
                        return true;
                    }
                }
            }
            fbm.UnlockBits();
            return false;
        }

        public void leftDown() {
            if (Program.useLeftKey)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(18);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
            else {
                rightDown();
            }
        }

        public void rightDown()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Thread.Sleep(18);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(int vKey);

        private bool keyIsDown(Keys k) {
            return GetAsyncKeyState((int)k) != 0;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);


        public const int MOUSEEVENTF_LEFTDOWN = 0x0002;

        public const int MOUSEEVENTF_LEFTUP = 0x0004;

        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;

        public const int MOUSEEVENTF_RIGHTUP = 0x0010;

        private void button1_Click(object sender, EventArgs e)
        {
            Program.useLeftKey = !Program.useLeftKey;
            button1.Text = Program.useLeftKey ? "左键模式" : "右键模式";
        }
    }
}
