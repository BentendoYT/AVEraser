using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AVEraser
{
    public enum ToastKind { Success, Info, Warning, Error }

    public class ToastNotification : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private const int W = 380;
        private const int H = 90;
        private const int ICON_SZ = 64;
        private const int PAD = 14;
        private const int SHOW_MS = 8000;
        private const int FADE_MS = 220;

        private static readonly Rectangle CLOSE_RECT = new Rectangle(W - 26, 8, 18, 18);

        private readonly string _title;
        private readonly string _body;
        private readonly ToastKind _kind;
        private readonly Image _icon;
        private readonly Action _onClick;
        private readonly Timer _showTimer = new Timer();
        private readonly Timer _fadeTimer = new Timer();
        private bool _closing;
        private double _alpha = 1.0;
        private bool _closeHover;

        private static int _stackOffset = 0;
        private const int STACK_GAP = H + 8;

        private ToastNotification(string title, string body, ToastKind kind, Action onClick)
        {
            _title = title;
            _body = body;
            _kind = kind;
            _icon = LoadIcon(kind);
            _onClick = onClick;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.ClientSize = new Size(W, H);
            this.BackColor = Color.FromArgb(22, 22, 22);
            this.Opacity = 1.0;
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer, true);

            var wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(
                wa.Right - W - 12,
                wa.Bottom - H - 12 - _stackOffset);
            _stackOffset += STACK_GAP;

            this.Load += (s, e) =>
            {
                int pref = DWMWCP_ROUND;
                if (DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref pref, sizeof(int)) != 0)
                {
                    Form1.SetRoundedRegion(this, 8);
                }
            };

            this.Paint += Draw;
            this.MouseMove += (s, e) =>
            {
                bool over = CLOSE_RECT.Contains(e.Location);
                if (over != _closeHover) { _closeHover = over; Invalidate(); }
            };
            this.MouseLeave += (s, e) => { _closeHover = false; Invalidate(); };
            this.MouseClick += (s, e) =>
            {
                if (CLOSE_RECT.Contains(((MouseEventArgs)e).Location))
                    BeginClose();
                else
                {
                    BeginClose();
                    _onClick?.Invoke();
                }
            };
            this.FormClosed += (s, e) =>
            {
                _stackOffset = Math.Max(0, _stackOffset - STACK_GAP);
                _icon?.Dispose();
            };

            _showTimer.Interval = SHOW_MS;
            _showTimer.Tick += (s, e) => { _showTimer.Stop(); BeginClose(); };

            _fadeTimer.Interval = 16;
            _fadeTimer.Tick += (s, e) =>
            {
                _alpha = Math.Max(0.0, _alpha - 16.0 / FADE_MS);
                this.Opacity = _alpha;
                if (_alpha <= 0) { _fadeTimer.Stop(); this.Close(); }
            };
        }

        public static void Show(string title, string body,
            ToastKind kind = ToastKind.Info, Action onClick = null)
        {
            if (Application.OpenForms.Count > 0 &&
                Application.OpenForms[0].InvokeRequired)
            {
                Application.OpenForms[0].BeginInvoke(
                    new Action(() => Show(title, body, kind, onClick)));
                return;
            }
            var t = new ToastNotification(title, body, kind, onClick);
            t.Show();
            t._showTimer.Start();
        }

        private void BeginClose()
        {
            if (_closing) return;
            _closing = true;
            _showTimer.Stop();
            _fadeTimer.Start();
        }

        private void Draw(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var b = new SolidBrush(Color.FromArgb(22, 22, 22)))
                g.FillRectangle(b, 0, 0, W, H);

            using (var pen = new Pen(Color.FromArgb(50, 50, 50), 1f))
                g.DrawRectangle(pen, 0, 0, W - 1, H - 1);

            if (_icon != null)
            {
                int iconX = PAD;
                int iconY = (H - ICON_SZ) / 2;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(_icon, new Rectangle(iconX, iconY, ICON_SZ, ICON_SZ));
                g.InterpolationMode = InterpolationMode.Default;
                g.PixelOffsetMode = PixelOffsetMode.Default;
            }

            int textX = PAD + ICON_SZ + 12;
            int textW = W - textX - 30;

            bool hasAction = _onClick != null;
            int titleY = hasAction ? 10 : 14;
            int bodyY = hasAction ? 30 : 38;

            using (var b = new SolidBrush(Color.FromArgb(245, 245, 245)))
            using (var f = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point))
                g.DrawString(_title, f, b, new RectangleF(textX, titleY, textW, 22));

            using (var b = new SolidBrush(Color.FromArgb(170, 170, 170)))
            using (var f = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point))
            using (var sf = new StringFormat
            { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.LineLimit })
                g.DrawString(_body, f, b, new RectangleF(textX, bodyY, textW, 36), sf);

            if (hasAction)
                using (var b = new SolidBrush(Color.FromArgb(80, 80, 85)))
                using (var f = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point))
                    g.DrawString("Click to open", f, b,
                        new RectangleF(textX, 64, textW, 16));

            if (_closeHover)
                using (var b = new SolidBrush(Color.FromArgb(55, 55, 55)))
                    g.FillEllipse(b, CLOSE_RECT);

            int cx = CLOSE_RECT.Left + CLOSE_RECT.Width / 2;
            int cy = CLOSE_RECT.Top + CLOSE_RECT.Height / 2;
            int arm = 4;
            using (var pen = new Pen(
                _closeHover ? Color.FromArgb(220, 220, 220) : Color.FromArgb(110, 110, 110), 1.5f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx - arm, cy - arm, cx + arm, cy + arm);
                g.DrawLine(pen, cx + arm, cy - arm, cx - arm, cy + arm);
            }
        }

        private static Image LoadIcon(ToastKind kind)
        {
            string name;
            switch (kind)
            {
                case ToastKind.Warning: name = "notifWarn"; break;
                case ToastKind.Error: name = "notifCrit"; break;
                default: name = "notifInfo"; break;
            }
            try
            {
                var obj = Properties.Resources.ResourceManager.GetObject(name);
                if (obj is Icon ico) return new Icon(ico, new Size(ICON_SZ, ICON_SZ)).ToBitmap();
                if (obj is Bitmap bmp) return bmp;
            }
            catch { }
            return new Bitmap(ICON_SZ, ICON_SZ);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000;
                return cp;
            }
        }
    }
}