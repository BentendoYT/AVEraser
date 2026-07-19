using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AVEraser
{
    public partial class Form1 : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string src, string dst, uint flags);
        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        internal static readonly Color C_Bg0 = Color.FromArgb(13, 15, 18);
        internal static readonly Color C_Bg1 = Color.FromArgb(19, 22, 28);
        internal static readonly Color C_Bg2 = Color.FromArgb(26, 30, 38);
        internal static readonly Color C_Bg3 = Color.FromArgb(32, 37, 48);
        internal static readonly Color C_Line = Color.FromArgb(38, 44, 56);
        internal static readonly Color C_Blue = Color.FromArgb(47, 119, 243);
        internal static readonly Color C_BlueH = Color.FromArgb(72, 140, 255);
        internal static readonly Color C_Green = Color.FromArgb(32, 195, 110);
        internal static readonly Color C_Warn = Color.FromArgb(240, 170, 45);
        internal static readonly Color C_Red = Color.FromArgb(220, 70, 70);
        internal static readonly Color C_Text0 = Color.FromArgb(235, 238, 248);
        internal static readonly Color C_Text1 = Color.FromArgb(140, 150, 170);
        internal static readonly Color C_Text2 = Color.FromArgb(65, 75, 95);

        internal static void SetRoundedRegion(Form form, int radius)
        {
            var path = new GraphicsPath();
            var r = form.ClientRectangle; int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); form.Region = new Region(path);
        }

        internal static readonly Dictionary<string, AVEntry> KnownAVs =
            new Dictionary<string, AVEntry>();

        private Dictionary<string, ScanResult> _lastResults;
        private List<string> _lastLog;
        private bool _lastNeedsReboot;

        private readonly bool _pendingDelete;
        private readonly bool _startBackground;

        private NotifyIcon _trayIcon;
        private TrayMonitor _monitor;
        private bool _isInstallerVersion;

        public Form1(bool pendingDelete = false, bool startBackground = false)
        {
            _pendingDelete = pendingDelete;
            _startBackground = startBackground;
            InitializeComponent();
            this.Text = "AVEraser";
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            if (_startBackground) { ShowInTaskbar = false; Opacity = 0; }
            Load += Form1_Load;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var b = new SolidBrush(C_Bg0))
            {
                int r = 8;
                e.Graphics.FillRectangle(b, 0, Height - r, r, r);
                e.Graphics.FillRectangle(b, Width - r, Height - r, r, r);
                e.Graphics.FillRectangle(b, 0, 0, r, r);
                e.Graphics.FillRectangle(b, Width - r, 0, r, r);
            }
        }

        private void HideToTray() => Hide();

        public void ShowMainWindow()
        {
            Opacity = 1; ShowInTaskbar = true;
            Show(); WindowState = FormWindowState.Normal;
            BringToFront(); Activate();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            BackColor = C_Bg0; pnlContent.BackColor = C_Bg1;
            pnlHeader.BackColor = C_Bg0; pnlFooter.BackColor = C_Bg0;
            int pref = 2;
            DwmSetWindowAttribute(Handle, 33, ref pref, sizeof(int));

            bool drag = false; Point ds = Point.Empty;
            pnlHeader.MouseDown += (s, ev) => { if (ev.Button == MouseButtons.Left) { drag = true; ds = ev.Location; } };
            pnlHeader.MouseMove += (s, ev) => { if (drag) { var sc = pnlHeader.PointToScreen(ev.Location); Location = new Point(sc.X - ds.X, sc.Y - ds.Y); } };
            pnlHeader.MouseUp += (s, ev) => drag = false;

            avGrid.CheckChanged += (s, ev) => btnDelete.Visible = avGrid.GetCheckedKeys().Count > 0;

            _isInstallerVersion = IsInstallerVersion();

            if (_isInstallerVersion)
            {
                InitTray();
                _monitor = new TrayMonitor(KnownAVs);
                _monitor.AVDetected += name =>
                {
                    if (IsHandleCreated)
                        BeginInvoke(new Action(() =>
                            ToastNotification.Show("Antivirus detected",
                                name + " residues appeared on your system.", ToastKind.Warning,
                                onClick: () => BeginInvoke(new Action(ShowMainWindow)))));
                };
                _monitor.Start();
            }

            if (_startBackground && _isInstallerVersion)
            {
                BeginInvoke(new Action(() => { Hide(); ShowInTaskbar = false; }));
                _ = LoadSignaturesAndNotifyMonitor();
                return;
            }

            if (_pendingDelete)
            {
                await Task.Delay(300);
                BtnScan_Click(this, EventArgs.Empty);
                while (!avGrid.Visible) await Task.Delay(100);
                await Task.Delay(300);
                avGrid.SetAllChecked(true);
                await Task.Delay(100);
                BtnDelete_Click(this, EventArgs.Empty);
            }
            else
            {
                await Task.Delay(600);
                await LoadSignaturesAndNotifyMonitor();
                await Task.Delay(400);
                _ = CheckForUpdatesAsync();
            }
        }

        private async Task LoadSignaturesAndNotifyMonitor()
        {
            await LoadCommunitySignaturesAsync();
            _monitor?.RefreshWatchers();
        }

        private static bool IsInstallerVersion()
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var key = hklm.OpenSubKey(@"SOFTWARE\AVEraser", false))
                    return key?.GetValue("InstallPath") != null;
            }
            catch { return false; }
        }

        private void InitTray()
        {
            Icon trayIco;
            try { trayIco = new Icon(Application.ExecutablePath, 16, 16); }
            catch { try { trayIco = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { trayIco = SystemIcons.Shield; } }

            _trayIcon = new NotifyIcon { Icon = trayIco, Text = "AVEraser", Visible = true };
            _trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ShowMainWindow(); return; }
            if (e.Button != MouseButtons.Right) return;

            bool runAtStartup = StartupDialog.NormExists() || StartupDialog.SvcExists();
            NativeMenu.GetCursorPos(out NativeMenu.POINT pt);

            var menu = new TrayContextMenu(runAtStartup, new Point(pt.X, pt.Y));
            menu.OnOpen += () => BeginInvoke(new Action(ShowMainWindow));
            menu.OnToggleStartup += () => BeginInvoke(new Action(ToggleStartup));
            menu.OnExit += () => BeginInvoke(new Action(TrulyExit));
            menu.Show();
            menu.Activate();
        }

        private void ToggleStartup()
        {
            try
            {
                if (StartupDialog.NormExists() || StartupDialog.SvcExists())
                    StartupDialog.RemoveAllStartup();
                else
                    StartupDialog.InstallSimpleStartup();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not change startup setting:\n\n" + ex.Message,
                    "AVEraser", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            if (_isInstallerVersion)
            {
                HideToTray();
            }
            else
            {
                TrulyExit();
            }
        }

        private void TrulyExit()
        {
            _monitor?.Stop();
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); _trayIcon = null; }
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isInstallerVersion && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; HideToTray(); return;
            }
            _monitor?.Stop();
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); _trayIcon = null; }
            base.OnFormClosing(e);
        }

        private void Minimize_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;
        private void BtnSelectAll_Click(object sender, EventArgs e) => avGrid.SetAllChecked(true);
        private void BtnDeselectAll_Click(object sender, EventArgs e) => avGrid.SetAllChecked(false);
        private void BtnReport_Click(object sender, EventArgs e) => SignatureDb.OpenReportPage();

        private const string LAST_DB_VER_KEY = @"SOFTWARE\AVEraser";
        private const string LAST_DB_VER_VALUE = "LastDbVersion";

        private async Task LoadCommunitySignaturesAsync()
        {
            try
            {
                var db = await SignatureDb.FetchAsync();
                if (db == null) return;
                SignatureDb.MergeInto(KnownAVs, db);

                string lastSeen = GetLastSeenDbVersion();
                if (db.DbVersion != lastSeen)
                {
                    SaveLastSeenDbVersion(db.DbVersion);
                    int newEntries = KnownAVs.Count;
                    string msg = "+" + newEntries + " signature" + (newEntries != 1 ? "s" : "") + " loaded";
                    if (!string.IsNullOrEmpty(lastSeen))
                        msg = "DB updated from v" + lastSeen + " to v" + db.DbVersion;
                    ToastNotification.Show("Signature DB updated", msg, ToastKind.Info);
                }
            }
            catch { }
        }

        private static string GetLastSeenDbVersion()
        {
            try
            {
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                using (var key = hkcu.OpenSubKey(LAST_DB_VER_KEY, false))
                    return key?.GetValue(LAST_DB_VER_VALUE) as string;
            }
            catch { return null; }
        }

        private static void SaveLastSeenDbVersion(string version)
        {
            try
            {
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                using (var key = hkcu.CreateSubKey(LAST_DB_VER_KEY, true))
                    key?.SetValue(LAST_DB_VER_VALUE, version ?? "");
            }
            catch { }
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var u = await Updater.CheckForUpdateAsync();
                if (u == null) return;
                Invoke(new Action(() => { using (var d = new UpdateDialog(u)) d.ShowDialog(this); }));
            }
            catch { }
        }

        private void BtnScan_Click(object sender, EventArgs e)
        {
            _lastResults = null;
            btnDelete.Visible = false; pnlSelectBar.Visible = false;
            btnScan.Enabled = false; avProgress.Visible = true; avProgress.Value = 0;
            lblStatus.Text = "Searching system for antivirus residues\u2026";
            avGrid.ClearItems(); avGrid.Visible = false;

            Task.Run(() =>
            {
                var results = new Dictionary<string, ScanResult>();
                int i = 0;
                foreach (var kv in KnownAVs)
                {
                    i++;
                    var av = kv.Value; int pct = (int)(i * 100.0 / KnownAVs.Count);
                    Invoke(new Action(() => { avProgress.Value = pct; lblStatus.Text = "Checking  " + av.DisplayName + "\u2026"; }));

                    var r = new ScanResult { AVKey = kv.Key, DisplayName = av.DisplayName };
                    foreach (var p in av.Processes) if (Process.GetProcessesByName(p).Length > 0) r.FoundProcesses.Add(p + ".exe");
                    foreach (var svc in av.Services) if (ServiceExists(svc)) r.FoundServices.Add(svc);
                    foreach (var reg in av.RegistryKeys) if (RegistryKeyExists(reg)) r.FoundRegistryKeys.Add(@"HKLM\" + reg);
                    foreach (var fld in av.Folders) if (Directory.Exists(fld)) r.FoundFolders.Add(fld);

                    foreach (var ba in av.BundledApps)
                    {
                        var br = new BundledScanResult { DisplayName = ba.DisplayName };
                        foreach (var p in ba.Processes) if (Process.GetProcessesByName(p).Length > 0) br.Processes.Add(p + ".exe");
                        foreach (var svc in ba.Services) if (ServiceExists(svc)) br.Services.Add(svc);
                        foreach (var reg in ba.RegistryKeys) if (RegistryKeyExists(reg)) br.RegistryKeys.Add(@"HKLM\" + reg);
                        foreach (var fld in ba.Folders) if (Directory.Exists(fld)) br.Folders.Add(fld);
                        if (br.HasAny) r.FoundBundled[ba.DisplayName] = br;
                    }
                    if (r.HasAnyFindings) results[kv.Key] = r;
                }

                Invoke(new Action(() =>
                {
                    _lastResults = results; avProgress.Visible = false;
                    btnScan.Enabled = true; avGrid.Visible = true;
                    if (results.Count == 0)
                    {
                        lblStatus.Text = "No residues found \u2014 system is clean.";
                        avGrid.ShowClean();
                    }
                    else
                    {
                        lblStatus.Text = results.Count + " product(s) with residues found. Select items and delete.";
                        foreach (var kv in results)
                        {
                            var r = kv.Value;
                            int total = r.FoundProcesses.Count + r.FoundServices.Count + r.FoundRegistryKeys.Count + r.FoundFolders.Count;
                            avGrid.AddRow(kv.Key, r.DisplayName, total, r.FoundProcesses.Count, r.FoundServices.Count, r.FoundRegistryKeys.Count, r.FoundFolders.Count);
                        }
                        pnlSelectBar.Visible = true;
                    }
                }));
            });
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_lastResults == null) return;
            var keys = avGrid.GetCheckedKeys();
            if (keys.Count == 0) return;

            using (var dlg = new Dialog("Remove residues?",
                "The residues of " + keys.Count + " product(s) will be permanently deleted.\n\n" +
                "Make sure you have uninstalled the antivirus program first via\n" +
                "Windows Settings \u2192 Apps.", "Delete", "Cancel"))
            { if (dlg.ShowDialog(this) != DialogResult.OK) return; }

            var bundled = new List<BundledScanResult>();
            foreach (string key in keys)
            {
                if (!_lastResults.TryGetValue(key, out ScanResult r2) || r2.FoundBundled.Count == 0) continue;
                string names = string.Join("\n  \u2022 ", r2.FoundBundled.Keys);
                using (var bd = new Dialog("Also remove bundled apps?",
                    "Residues of apps bundled with " + r2.DisplayName + " were found:\n\n  \u2022 " + names + "\n\nRemove these as well?",
                    "Yes, remove", "Skip"))
                { if (bd.ShowDialog(this) == DialogResult.OK) foreach (var bv in r2.FoundBundled.Values) bundled.Add(bv); }
            }

            var log = new List<string>();
            bool reboot = false;
            btnDelete.Visible = false; btnScan.Enabled = false;
            avProgress.Value = 0; avProgress.Visible = true;
            pnlSelectBar.Visible = false; avGrid.Visible = false;

            Task.Run(() =>
            {
                int done = 0;
                foreach (string key in keys)
                {
                    if (!_lastResults.TryGetValue(key, out ScanResult r)) continue;
                    done++;
                    Invoke(new Action(() => { avProgress.Value = (int)(done * 100.0 / keys.Count); lblStatus.Text = "Removing  " + r.DisplayName + "\u2026"; }));

                    foreach (var svc in r.FoundServices) RunCmd("sc config \"" + svc + "\" start= disabled");
                    foreach (var p in r.FoundProcesses)
                    {
                        try { var ps = Process.GetProcessesByName(p.Replace(".exe", "")); if (ps.Length == 0) { log.Add("SKIP|Process not running|" + p); continue; } foreach (var proc in ps) { proc.Kill(); proc.WaitForExit(3000); } log.Add("OK|Process terminated|" + p); }
                        catch (Exception ex) { log.Add("WARN|Process|" + p + ": " + ex.Message); }
                    }
                    foreach (var svc in r.FoundServices)
                    {
                        try { RunCmd("sc stop \"" + svc + "\""); Thread.Sleep(800); RunCmd("sc delete \"" + svc + "\""); log.Add("OK|Service removed|" + svc); }
                        catch (Exception ex) { log.Add("ERR|Service|" + svc + ": " + ex.Message); }
                    }
                    Thread.Sleep(500);
                    foreach (var reg in r.FoundRegistryKeys)
                    {
                        try { string sub = reg.StartsWith(@"HKLM\") ? reg.Substring(5) : reg; using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)) hklm.DeleteSubKeyTree(sub, throwOnMissingSubKey: false); log.Add("OK|Registry deleted|" + reg); }
                        catch (Exception ex) { log.Add("ERR|Registry|" + reg + ": " + ex.Message); }
                    }
                    foreach (var fld in r.FoundFolders)
                    {
                        if (!Directory.Exists(fld)) { log.Add("SKIP|Folder not found|" + fld); continue; }
                        if (TryDeleteFolder(fld)) { log.Add("OK|Folder deleted|" + fld); continue; }
                        RunCmd("takeown /F \"" + fld + "\" /R /D Y >nul 2>&1");
                        RunCmd("icacls \"" + fld + "\" /grant administrators:F /T /Q >nul 2>&1");
                        if (TryDeleteFolder(fld)) { log.Add("OK|Folder force-deleted|" + fld); continue; }
                        try { int n = ScheduleBootDelete(fld); reboot = true; log.Add("OK|Scheduled boot-time deletion (" + n + " files)|" + fld); }
                        catch (Exception ex) { log.Add("ERR|Folder|" + fld + ": " + ex.Message); }
                    }
                }

                foreach (var br in bundled)
                {
                    Invoke(new Action(() => { lblStatus.Text = "Removing bundled: " + br.DisplayName + "\u2026"; }));
                    foreach (var p in br.Processes) { try { var ps = Process.GetProcessesByName(p.Replace(".exe", "")); foreach (var proc in ps) { proc.Kill(); proc.WaitForExit(3000); } if (ps.Length > 0) log.Add("OK|Bundled process terminated|" + p); } catch (Exception ex) { log.Add("WARN|Bundled process|" + p + ": " + ex.Message); } }
                    foreach (var svc in br.Services) { RunCmd("sc stop \"" + svc + "\""); Thread.Sleep(500); RunCmd("sc delete \"" + svc + "\""); log.Add("OK|Bundled service removed|" + svc); }
                    foreach (var reg in br.RegistryKeys) { try { string sub = reg.StartsWith(@"HKLM\") ? reg.Substring(5) : reg; using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)) hklm.DeleteSubKeyTree(sub, throwOnMissingSubKey: false); log.Add("OK|Bundled registry deleted|" + reg); } catch (Exception ex) { log.Add("ERR|Bundled registry|" + reg + ": " + ex.Message); } }
                    foreach (var fld in br.Folders) { if (!Directory.Exists(fld)) continue; RunCmd("takeown /F \"" + fld + "\" /R /D Y >nul 2>&1"); RunCmd("icacls \"" + fld + "\" /grant administrators:F /T /Q >nul 2>&1"); if (TryDeleteFolder(fld)) { log.Add("OK|Bundled folder deleted|" + fld); continue; } int nb = ScheduleBootDelete(fld); reboot = true; log.Add("OK|Bundled folder boot-scheduled (" + nb + " files)|" + fld); }
                }

                bool fr = reboot; var fl = log;
                Invoke(new Action(() =>
                {
                    _lastLog = fl; _lastNeedsReboot = fr;
                    avProgress.Visible = false; btnScan.Enabled = true;

                    Action openReport = () =>
                    {
                        ShowMainWindow();
                        using (var rep = new Report("Cleanup complete", _lastLog, _lastNeedsReboot))
                            rep.ShowDialog(this);
                        BtnScan_Click(this, EventArgs.Empty);
                    };

                    if (fr)
                        ToastNotification.Show("Cleanup complete \u2014 reboot required",
                            "Some locked files are scheduled for boot-time deletion.",
                            ToastKind.Warning, () => BeginInvoke(new Action(openReport)));
                    else if (fl.FindAll(l => l.StartsWith("ERR")).Count > 0)
                        ToastNotification.Show("Cleanup finished with errors",
                            "Some items could not be removed. Check the report.",
                            ToastKind.Error, () => BeginInvoke(new Action(openReport)));
                    else
                        ToastNotification.Show("Cleanup complete",
                            keys.Count + " product" + (keys.Count > 1 ? "s" : "") + " successfully removed.",
                            ToastKind.Success, () => BeginInvoke(new Action(openReport)));

                    openReport();
                }));
            });
        }

        private static string _sDeletePath;
        private static string EnsureSDelete()
        {
            if (_sDeletePath != null) return _sDeletePath;
            string dest = Path.Combine(Path.GetTempPath(), "sdelete64.exe");
            if (File.Exists(dest)) { _sDeletePath = dest; return dest; }
            try { using (var wc = new System.Net.WebClient()) { wc.Headers["User-Agent"] = "AVEraser"; wc.DownloadFile("https://live.sysinternals.com/sdelete64.exe", dest); } Registry.SetValue(@"HKEY_CURRENT_USER\Software\Sysinternals\SDelete", "EulaAccepted", 1); _sDeletePath = dest; return dest; }
            catch { return null; }
        }

        private static bool TryDeleteFolder(string path)
        {
            try { Directory.Delete(path, true); return true; } catch { }
            try { RunCmd("rd /s /q \"" + path + "\""); if (!Directory.Exists(path)) return true; } catch { }
            try { RunCmd("powershell -NoProfile -NonInteractive -Command \"Remove-Item -LiteralPath '" + path.Replace("'", "''") + "' -Recurse -Force -ErrorAction SilentlyContinue\""); if (!Directory.Exists(path)) return true; } catch { }
            try { string sd = EnsureSDelete(); if (sd != null) { RunCmd("\"" + sd + "\" -s -r -q \"" + path + "\""); if (!Directory.Exists(path)) return true; } } catch { }
            return false;
        }

        private static int ScheduleBootDelete(string folderPath)
        {
            int count = 0;
            foreach (string f in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            { try { MoveFileEx(f, null, MOVEFILE_DELAY_UNTIL_REBOOT); count++; } catch { } }
            var dirs = new List<string>(Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories));
            dirs.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (string d in dirs) try { MoveFileEx(d, null, MOVEFILE_DELAY_UNTIL_REBOOT); } catch { }
            try { MoveFileEx(folderPath, null, MOVEFILE_DELAY_UNTIL_REBOOT); } catch { }
            return count;
        }

        private static bool RegistryKeyExists(string path)
        {
            try { using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)) using (var key = hklm.OpenSubKey(path, false)) return key != null; }
            catch { return false; }
        }

        private static bool ServiceExists(string name)
        {
            try { var psi = new ProcessStartInfo("sc.exe", "query \"" + name + "\"") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true }; using (var p = Process.Start(psi)) { string o = p.StandardOutput.ReadToEnd(); p.WaitForExit(); return p.ExitCode == 0 && o.Contains("SERVICE_NAME"); } }
            catch { return false; }
        }

        private static void RunCmd(string cmd)
        { var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd) { CreateNoWindow = true, UseShellExecute = false }; using (var p = Process.Start(psi)) p.WaitForExit(); }
    }

    internal static class NativeMenu
    {
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT pt);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }
    }

    public class TrayMonitor
    {
        public event Action<string> AVDetected;

        private readonly Dictionary<string, AVEntry> _db;
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly object _lock = new object();

        private readonly HashSet<string> _baselineFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _baselineRegKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private CancellationTokenSource _cts;
        private Thread _pollThread;

        public TrayMonitor(Dictionary<string, AVEntry> db) { _db = db; }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            WatchPath(@"C:\Program Files");
            WatchPath(@"C:\Program Files (x86)");
            WatchPath(@"C:\ProgramData");

            _pollThread = new Thread(PollLoop)
            { IsBackground = true, Priority = ThreadPriority.Lowest, Name = "AVEraser-Monitor" };
            _pollThread.Start();
        }

        public void RefreshWatchers()
        {
            lock (_lock)
            {
                foreach (var kv in _db)
                {
                    foreach (string fld in kv.Value.Folders)
                        if (Directory.Exists(fld))
                            _baselineFolders.Add(fld.TrimEnd('\\').ToLowerInvariant());

                    foreach (string reg in kv.Value.RegistryKeys)
                        if (RegExists(reg))
                            _baselineRegKeys.Add(reg.ToLowerInvariant());
                }

                foreach (var kv in _db)
                    foreach (string fld in kv.Value.Folders)
                    {
                        string parent = Path.GetDirectoryName(fld);
                        if (!string.IsNullOrEmpty(parent)) WatchPath(parent);
                    }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            lock (_lock)
            {
                foreach (var w in _watchers) { w.EnableRaisingEvents = false; w.Dispose(); }
                _watchers.Clear();
            }
        }

        private void OnCreated(object s, FileSystemEventArgs e) => CheckPath(e.FullPath);
        private void OnRenamed(object s, RenamedEventArgs e) => CheckPath(e.FullPath);

        private void CheckPath(string fullPath)
        {
            string norm = fullPath.TrimEnd('\\').ToLowerInvariant();

            if (_baselineFolders.Contains(norm)) return;

            foreach (var kv in _db)
            {
                if (_reported.Contains(kv.Key)) continue;
                foreach (string folder in kv.Value.Folders)
                {
                    if (string.Equals(norm, folder.TrimEnd('\\').ToLowerInvariant()))
                    {
                        if (_reported.Add(kv.Key))
                            AVDetected?.Invoke(kv.Value.DisplayName);
                        return;
                    }
                }
            }
        }

        private void PollLoop()
        {
            if (_cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(30))) return;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    foreach (var kv in _db)
                    {
                        if (_reported.Contains(kv.Key)) continue;

                        bool hit = false;

                        foreach (string reg in kv.Value.RegistryKeys)
                        {
                            string normReg = reg.ToLowerInvariant();
                            if (_baselineRegKeys.Contains(normReg)) continue;
                            if (RegExists(reg)) { hit = true; break; }
                        }

                        if (!hit)
                        {
                            foreach (string fld in kv.Value.Folders)
                            {
                                string normFld = fld.TrimEnd('\\').ToLowerInvariant();
                                if (_baselineFolders.Contains(normFld)) continue;
                                if (Directory.Exists(fld)) { hit = true; break; }
                            }
                        }

                        if (hit && _reported.Add(kv.Key))
                            AVDetected?.Invoke(kv.Value.DisplayName);
                    }
                }
                catch { }
                for (int i = 0; i < 300 && !_cts.Token.IsCancellationRequested; i++)
                    _cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
            }
        }

        private void WatchPath(string path)
        {
            if (!Directory.Exists(path)) return;
            lock (_lock)
            {
                foreach (var w in _watchers)
                    if (string.Equals(w.Path, path, StringComparison.OrdinalIgnoreCase)) return;
                try
                {
                    var watcher = new FileSystemWatcher(path)
                    {
                        NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName,
                        IncludeSubdirectories = false,
                        EnableRaisingEvents = true
                    };
                    watcher.Created += OnCreated;
                    watcher.Renamed += OnRenamed;
                    _watchers.Add(watcher);
                }
                catch { }
            }
        }

        private static bool RegExists(string path)
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var key = hklm.OpenSubKey(path, false))
                    return key != null;
            }
            catch { return false; }
        }
    }

    internal class TrayMenuRow : Control
    {
        public bool ShowCheck { get; set; } = false;
        public bool Checked { get; set; } = false;
        private bool _hover;

        public TrayMenuRow()
        {
            Height = 34;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Default;
            MouseEnter += (s, e) => { _hover = true; Invalidate(); };
            MouseLeave += (s, e) => { _hover = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            if (_hover)
                using (var b = new SolidBrush(Form1.C_Bg3)) Gfx.FillRound(g, b, new Rectangle(4, 2, Width - 8, Height - 4), 6);

            int textX = 16;
            if (ShowCheck)
            {
                var cb = new Rectangle(14, (Height - 16) / 2, 16, 16);
                using (var path = Gfx.RoundRect(cb, 4))
                {
                    if (Checked)
                    {
                        using (var b = new SolidBrush(Form1.C_Blue)) g.FillPath(b, path);
                        using (var p = new Pen(Color.White, 1.8f))
                        { g.DrawLine(p, cb.Left + 3, cb.Top + 8, cb.Left + 7, cb.Bottom - 4); g.DrawLine(p, cb.Left + 7, cb.Bottom - 4, cb.Right - 2, cb.Top + 3); }
                    }
                    else using (var p = new Pen(Form1.C_Line, 1.3f)) g.DrawPath(p, path);
                }
                textX = 40;
            }

            using (var b = new SolidBrush(Form1.C_Text0))
            using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
                g.DrawString(Text, new Font("Segoe UI Variable Text", 9F), b, new RectangleF(textX, 0, Width - textX - 10, Height), sf);
        }
    }

    internal class TrayContextMenu : Form
    {
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);

        public event Action OnOpen;
        public event Action OnToggleStartup;
        public event Action OnExit;

        public TrayContextMenu(bool startupChecked, Point screenPos)
        {
            const int W = 200, ROW_H = 34, SEP_H = 9;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Form1.C_Bg1;
            TopMost = true;
            ClientSize = new Size(W, ROW_H * 3 + SEP_H * 2 + 8);
            Form1.SetRoundedRegion(this, 10);
            DialogHelper.ApplyAppIcon(this);
            Load += (s, e) => { int p = 2; DwmSetWindowAttribute(Handle, 33, ref p, sizeof(int)); Form1.SetRoundedRegion(this, 10); };
            Deactivate += (s, e) => Close();

            var screen = Screen.FromPoint(screenPos).WorkingArea;
            int x = Math.Min(screenPos.X, screen.Right - W - 4);
            int y = Math.Min(screenPos.Y - ClientSize.Height, screen.Bottom - ClientSize.Height - 4);
            Location = new Point(Math.Max(x, screen.Left), Math.Max(y, screen.Top));

            Paint += (s, e) => { using (var pen = new Pen(Form1.C_Line)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1); };

            int cy = 4;
            var rowOpen = new TrayMenuRow { Text = "Open AVEraser", Location = new Point(0, cy), Size = new Size(W, ROW_H) };
            rowOpen.Click += (s, e) => { OnOpen?.Invoke(); Close(); };
            cy += ROW_H;

            var sep1 = new Panel { Location = new Point(8, cy + SEP_H / 2), Size = new Size(W - 16, 1), BackColor = Form1.C_Line };
            cy += SEP_H;

            var rowStartup = new TrayMenuRow { Text = "Run at startup", ShowCheck = true, Checked = startupChecked, Location = new Point(0, cy), Size = new Size(W, ROW_H) };
            rowStartup.Click += (s, e) => { OnToggleStartup?.Invoke(); Close(); };
            cy += ROW_H;

            var sep2 = new Panel { Location = new Point(8, cy + SEP_H / 2), Size = new Size(W - 16, 1), BackColor = Form1.C_Line };
            cy += SEP_H;

            var rowExit = new TrayMenuRow { Text = "Exit", Location = new Point(0, cy), Size = new Size(W, ROW_H) };
            rowExit.Click += (s, e) => { OnExit?.Invoke(); Close(); };

            Controls.AddRange(new Control[] { rowOpen, sep1, rowStartup, sep2, rowExit });
        }
    }

    internal class StartupDialog : Form
    {
        private bool _runEnabled;
        private bool _highEnabled;

        private readonly ToggleSwitch _togRun;
        private readonly ToggleSwitch _togHigh;

        private const string SVC = "AVEraserStartup";
        private const string RKEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RVAL = "AVEraser";

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);

        public StartupDialog()
        {
            const int W = 440, pad = 28;
            Text = "AVEraser"; FormBorderStyle = FormBorderStyle.None;
            BackColor = Form1.C_Bg0; StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false; ClientSize = new Size(W, 330);
            Form1.SetRoundedRegion(this, 14);
            DialogHelper.ApplyAppIcon(this);
            Load += (s, e) => { int p = 2; DwmSetWindowAttribute(Handle, 33, ref p, sizeof(int)); Form1.SetRoundedRegion(this, 14); };

            Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var grad = new LinearGradientBrush(new Rectangle(0, 0, W, 74), Form1.C_Blue, Form1.C_BlueH, LinearGradientMode.Horizontal))
                    Gfx.FillRound(g, grad, new Rectangle(0, 0, W, 74), 14);
                using (var b = new SolidBrush(Form1.C_Blue)) g.FillRectangle(b, 0, 54, W, 20);
                using (var b = new SolidBrush(Form1.C_Bg0)) g.FillRectangle(b, 0, 60, W, 14);
                using (var b = new SolidBrush(Color.White))
                using (var f = new Font("Segoe UI Variable Display", 12F, FontStyle.Bold))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
                    g.DrawString("Automatic startup", f, b, new RectangleF(pad, 0, W - pad * 2, 58), sf);
                using (var pen = new Pen(Form1.C_Line)) g.DrawRectangle(pen, 0, 0, W - 1, Height - 1);
            };

            bool isHP = SvcExists();
            bool isNorm = NormExists();
            _runEnabled = isHP || isNorm;
            _highEnabled = isHP;

            var lblRun = new Label
            {
                Text = "Run with Windows",
                Location = new Point(pad, 82),
                AutoSize = true,
                ForeColor = Form1.C_Text0,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Variable Text", 10F)
            };
            var lblRunSub = new Label
            {
                Text = "Start AVEraser automatically when Windows starts",
                Location = new Point(pad, 103),
                AutoSize = true,
                ForeColor = Form1.C_Text2,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Variable Text", 8F)
            };
            _togRun = new ToggleSwitch { Location = new Point(W - pad - 48, 88), Checked = _runEnabled };
            _togRun.CheckedChanged += (s, e) =>
            {
                _runEnabled = _togRun.Checked;
                if (!_runEnabled) { _highEnabled = false; _togHigh.Checked = false; }
                _togHigh.Enabled = _runEnabled;
            };

            var sep = new Panel { Location = new Point(pad, 128), Size = new Size(W - pad * 2, 1), BackColor = Form1.C_Line };

            var lblHigh = new Label
            {
                Text = "High priority",
                Location = new Point(pad, 140),
                AutoSize = true,
                ForeColor = Form1.C_Text0,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Variable Text", 10F)
            };
            var lblHighSub = new Label
            {
                Text = "Uses a Windows service for faster startup",
                Location = new Point(pad, 161),
                AutoSize = true,
                ForeColor = Form1.C_Text2,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Variable Text", 8F)
            };
            _togHigh = new ToggleSwitch { Location = new Point(W - pad - 48, 146), Checked = _highEnabled, Enabled = _runEnabled };
            _togHigh.CheckedChanged += (s, e) => _highEnabled = _togHigh.Checked;

            var card = new Panel { Location = new Point(pad, 186), Size = new Size(W - pad * 2, 78), BackColor = Color.Transparent };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                Gfx.FillRound(g, new SolidBrush(Form1.C_Bg1), new Rectangle(0, 0, card.Width, card.Height), 10);
                using (var pen = new Pen(Form1.C_Line)) Gfx.DrawRound(g, pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
                string txt = "High priority starts AVEraser faster by registering a Windows service.\n" +
                             "This also starts AVEraser for all user accounts on this PC.\n" +
                             "Disable if you experience compatibility issues.";
                using (var b = new SolidBrush(Form1.C_Text2))
                using (var f = new Font("Segoe UI Variable Text", 8.5F))
                    g.DrawString(txt, f, b, new RectangleF(14, 10, card.Width - 28, card.Height - 20));
            };

            var btnApply = new RoundBtn
            {
                Text = "Apply",
                Location = new Point(W - pad - 100, 280),
                Size = new Size(100, 34),
                NormalColor = Form1.C_Blue,
                HoverColor = Form1.C_BlueH,
                ForeColor = Color.White,
                CornerRadius = 10,
                Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Bold),
                Cursor = Cursors.Default
            };
            var btnCancel = new RoundBtn
            {
                Text = "Cancel",
                Location = new Point(W - pad - 210, 280),
                Size = new Size(100, 34),
                NormalColor = Form1.C_Bg2,
                HoverColor = Form1.C_Bg3,
                ForeColor = Form1.C_Text1,
                CornerRadius = 10,
                Font = new Font("Segoe UI Variable Text", 9F),
                Cursor = Cursors.Default
            };
            btnApply.Click += Apply_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lblRun, lblRunSub, _togRun, sep, lblHigh, lblHighSub, _togHigh, card, btnApply, btnCancel });
            DialogHelper.EnableDrag(this);
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            try
            {
                if (_runEnabled)
                { if (_highEnabled) { RemNorm(); InstSvc(); } else { RemSvc(); InstNorm(); } }
                else { RemSvc(); RemNorm(); }
                DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not apply:\n\n" + ex.Message +
                    "\n\nMake sure AVEraser runs as Administrator.",
                    "AVEraser", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        internal static bool SvcExists() { try { using (var sc = new System.ServiceProcess.ServiceController(SVC)) { var _ = sc.Status; return true; } } catch { return false; } }
        internal static bool NormExists() { try { using (var k = Registry.CurrentUser.OpenSubKey(RKEY, false)) return k?.GetValue(RVAL) != null; } catch { return false; } }
        internal static void InstSvc() { Run("sc create \"" + SVC + "\" binPath= \"\\\"" + Application.ExecutablePath + "\\\" --background\" start= auto DisplayName= \"AVEraser Startup\""); Run("sc description \"" + SVC + "\" \"Starts AVEraser background monitor with Windows\""); Run("sc start \"" + SVC + "\""); }
        internal static void RemSvc() { Run("sc stop \"" + SVC + "\" >nul 2>&1"); Thread.Sleep(500); Run("sc delete \"" + SVC + "\" >nul 2>&1"); }
        internal static void InstNorm() { using (var k = Registry.CurrentUser.OpenSubKey(RKEY, true)) k?.SetValue(RVAL, "\"" + Application.ExecutablePath + "\" --background"); }
        internal static void RemNorm() { try { using (var k = Registry.CurrentUser.OpenSubKey(RKEY, true)) k?.DeleteValue(RVAL, false); } catch { } }
        internal static void Run(string cmd) { var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd) { CreateNoWindow = true, UseShellExecute = false }; using (var p = Process.Start(psi)) p.WaitForExit(); }

        internal static void InstallSimpleStartup() { RemSvc(); InstNorm(); }
        internal static void RemoveAllStartup() { RemSvc(); RemNorm(); }
    }

    internal class ToggleSwitch : Control
    {
        private bool _checked;
        private bool _hover;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return _checked; }
            set { if (_checked == value) return; _checked = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
        }

        public ToggleSwitch()
        {
            Size = new Size(48, 26);
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            MouseEnter += (s, e) => { _hover = true; Invalidate(); };
            MouseLeave += (s, e) => { _hover = false; Invalidate(); };
            MouseClick += (s, e) =>
            {
                if (((MouseEventArgs)e).Button == MouseButtons.Left && Enabled)
                    Checked = !Checked;
            };
            EnabledChanged += (s, e) => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color trackColor;
            if (!Enabled)
                trackColor = Color.FromArgb(40, 44, 55);
            else if (_checked)
                trackColor = _hover ? Form1.C_BlueH : Form1.C_Blue;
            else
                trackColor = _hover ? Form1.C_Bg3 : Form1.C_Bg2;

            var trackRect = new Rectangle(0, 3, Width - 1, Height - 7);
            using (var b = new SolidBrush(trackColor))
                Gfx.FillRound(g, b, trackRect, (trackRect.Height) / 2);

            using (var pen = new Pen(Enabled ? Form1.C_Line : Color.FromArgb(30, 35, 45), 1f))
                Gfx.DrawRound(g, pen, trackRect, trackRect.Height / 2);

            int thumbSize = Height - 8;
            int thumbX = _checked ? Width - thumbSize - 4 : 4;
            int thumbY = (Height - thumbSize) / 2;
            Color thumbColor = Enabled ? Color.White : Color.FromArgb(80, 90, 110);
            using (var b = new SolidBrush(thumbColor))
                g.FillEllipse(b, thumbX, thumbY, thumbSize, thumbSize);
        }
    }

    public class Report : Form
    {
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);

        private readonly Panel _pnlSummary;
        private readonly Panel _pnlLog;
        private readonly Label _lblTitle;
        private readonly Label _lblSub;

        public Report(string title, List<string> log, bool needsReboot = false)
        {
            const int W = 480, pad = 28;
            int ok = 0, err = 0, warn = 0, skip = 0;
            foreach (string l in log)
            { if (l.StartsWith("OK")) ok++; else if (l.StartsWith("SKIP") || l.StartsWith("INFO")) skip++; else if (l.StartsWith("WARN")) warn++; else err++; }

            int total = ok + err + warn + skip;
            string headline = err > 0 ? "Cleanup finished with errors" : needsReboot ? "Cleanup complete, reboot needed" : "Cleanup complete";
            Color numColor = Color.FromArgb(0x8C, 0x96, 0xAA);

            const int H = 420;
            Text = "AVEraser"; ClientSize = new Size(W, H);
            FormBorderStyle = FormBorderStyle.None; BackColor = Form1.C_Bg1;
            StartPosition = FormStartPosition.CenterParent;
            Form1.SetRoundedRegion(this, 12);
            DialogHelper.ApplyAppIcon(this);
            Load += (s, ev) => { int p = 2; DwmSetWindowAttribute(Handle, 33, ref p, sizeof(int)); Form1.SetRoundedRegion(this, 12); };

            Paint += (s, e) =>
            {
                using (var pen = new Pen(Form1.C_Line, 1f)) e.Graphics.DrawRectangle(pen, 0, 0, W - 1, H - 1);
            };

            _lblTitle = new Label
            {
                Text = headline,
                Font = new Font("Segoe UI Variable Display", 19F, FontStyle.Bold),
                ForeColor = Form1.C_Text0,
                Location = new Point(pad, 24),
                Size = new Size(W - pad * 2, 40),
                BackColor = Color.Transparent
            };
            _lblSub = new Label
            {
                Text = total + " item" + (total != 1 ? "s" : "") + " processed",
                Font = new Font("Segoe UI Variable Text", 8.5F),
                ForeColor = Form1.C_Text2,
                Location = new Point(pad, 64),
                Size = new Size(W - pad * 2, 18),
                BackColor = Color.Transparent
            };

            var sep = new Panel { Location = new Point(pad, 92), Size = new Size(W - pad * 2, 1), BackColor = Form1.C_Line };

            int contentTop = 108, contentH = H - contentTop - 70;

            _pnlSummary = new Panel { Location = new Point(pad, contentTop), Size = new Size(W - pad * 2, contentH), BackColor = Color.Transparent };
            AddSummaryRow(_pnlSummary, 0, "Succeeded", ok.ToString(), numColor);
            AddSummaryRow(_pnlSummary, 1, "Failed", err.ToString(), numColor);
            AddSummaryRow(_pnlSummary, 2, "Warnings", warn.ToString(), numColor);
            AddSummaryRow(_pnlSummary, 3, "Skipped", skip.ToString(), numColor);

            _pnlLog = new Panel { Location = new Point(pad, contentTop), Size = new Size(W - pad * 2, contentH), BackColor = Color.Transparent, Visible = false, AutoScroll = true };
            int ly = 0;
            foreach (string line in log)
            {
                if (line.StartsWith("INFO")) continue;
                var parts = line.Split('|');
                string txt = parts.Length > 2 ? parts[1] + "  " + parts[2] : line.Substring(Math.Min(4, line.Length));

                var lblTxt = new Label { Text = txt, Font = new Font("Segoe UI Variable Text", 8.5F), ForeColor = Form1.C_Text1, Location = new Point(0, ly), Size = new Size(_pnlLog.Width, 22), AutoEllipsis = true, BackColor = Color.Transparent };
                _pnlLog.Controls.Add(lblTxt);
                ly += 24;
            }

            int btnY = contentTop + contentH + 18;

            var btnClose = new RoundBtn { Text = "Close", Location = new Point(W - pad - 100, btnY), Size = new Size(100, 34), Outlined = true, OutlineColor = Form1.C_Text2, ForeColor = Form1.C_Text0, CornerRadius = 9, Font = new Font("Segoe UI Variable Text", 9F), Cursor = Cursors.Default };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            var btnViewLog = new RoundBtn { Text = "View Log", Location = new Point(W - pad - 214, btnY), Size = new Size(106, 34), NormalColor = Form1.C_Blue, HoverColor = Form1.C_BlueH, ForeColor = Color.White, CornerRadius = 9, Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Bold), Cursor = Cursors.Default };

            btnViewLog.Click += (s, e) =>
            {
                bool showingLog = _pnlLog.Visible;
                _pnlLog.Visible = !showingLog;
                _pnlSummary.Visible = showingLog;
                _lblTitle.Text = showingLog ? headline : "Cleanup Log";
                _lblSub.Text = showingLog ? (total + " item" + (total != 1 ? "s" : "") + " processed") : "Completed just now";
                btnViewLog.Text = showingLog ? "View Log" : "Back";
            };

            Controls.AddRange(new Control[] { _lblTitle, _lblSub, sep, _pnlSummary, _pnlLog, btnViewLog, btnClose });

            if (needsReboot)
            {
                var btnRbt = new RoundBtn { Text = "\u21ba  Reboot", Location = new Point(pad, btnY), Size = new Size(100, 34), NormalColor = Color.FromArgb(130, 70, 5), HoverColor = Color.FromArgb(175, 100, 10), ForeColor = Color.White, CornerRadius = 9, Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Bold), Cursor = Cursors.Default };
                btnRbt.Click += (s, e) => { Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 10 /c \"AVEraser: completing cleanup\"") { UseShellExecute = true }); Application.Exit(); };
                Controls.Add(btnRbt);
            }

            DialogHelper.EnableDrag(this);
        }

        private static void AddSummaryRow(Panel parent, int index, string label, string value, Color valueColor)
        {
            const int rowH = 32;
            int y = index * rowH;

            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI Variable Text", 9.5F),
                ForeColor = Form1.C_Text1,
                Location = new Point(0, y),
                Size = new Size(parent.Width - 80, rowH),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            var val = new Label
            {
                Text = value,
                Font = new Font("Segoe UI Variable Text", 9.5F, FontStyle.Bold),
                ForeColor = valueColor,
                Location = new Point(parent.Width - 80, y),
                Size = new Size(80, rowH),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lbl);
            parent.Controls.Add(val);

            var line = new Panel { Location = new Point(0, y + rowH - 1), Size = new Size(parent.Width, 1), BackColor = Form1.C_Line };
            parent.Controls.Add(line);
        }
    }

    public class UpdateInfo { public string Version, Url, Changelog; }

    public static class Updater
    {
        private const string VER_URL = "https://raw.githubusercontent.com/BentendoYT/AVEraser/main/version.json";
        public static string CurrentVersion => Application.ProductVersion;
        public static async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                using (var c = new HttpClient())
                {
                    c.Timeout = TimeSpan.FromSeconds(8);
                    c.DefaultRequestHeaders.Add("User-Agent", "AVEraser-Updater");
                    string json = await c.GetStringAsync(VER_URL);
                    string v = Rx(json, "version"), u = Rx(json, "url"), cl = Rx(json, "changelog");
                    if (string.IsNullOrWhiteSpace(v) || string.IsNullOrWhiteSpace(u)) return null;
                    try { if (new Version(v.Trim()) <= new Version(CurrentVersion.Trim())) return null; } catch { return null; }
                    return new UpdateInfo { Version = v, Url = u, Changelog = cl };
                }
            }
            catch { return null; }
        }
        private static string Rx(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return m.Success ? m.Groups[1].Value.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\") : null;
        }
        public static async Task DownloadAndInstallAsync(UpdateInfo info, Action<int> onProgress)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "AVEraserUpdate.exe");
            using (var c = new HttpClient())
            {
                c.DefaultRequestHeaders.Add("User-Agent", "AVEraser-Updater");
                using (var resp = await c.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    resp.EnsureSuccessStatusCode();
                    long total = resp.Content.Headers.ContentLength ?? -1, done = 0;
                    using (var s = await resp.Content.ReadAsStreamAsync())
                    using (var f = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    { byte[] buf = new byte[65536]; int n; while ((n = await s.ReadAsync(buf, 0, buf.Length)) > 0) { await f.WriteAsync(buf, 0, n); done += n; if (total > 0) onProgress?.Invoke((int)(done * 100 / total)); } }
                }
            }
            Process.Start(new ProcessStartInfo(tmp, "/VERYSILENT /SUPPRESSMSGBOXES /RESTART") { UseShellExecute = true });
            Application.Exit();
        }
    }

    public class UpdateDialog : Form
    {
        private readonly ProgressBar _bar; private readonly Label _lbl; private readonly RoundBtn _btnU, _btnS; private readonly UpdateInfo _info;
        public UpdateDialog(UpdateInfo info)
        {
            _info = info; const int W = 480, pad = 28, HDR = 52;
            Text = "AVEraser"; FormBorderStyle = FormBorderStyle.None; BackColor = Form1.C_Bg1;
            StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false; ClientSize = new Size(W, 256);
            Form1.SetRoundedRegion(this, 14);
            DialogHelper.ApplyAppIcon(this);
            Paint += (s, e) => DialogHelper.PaintHeader(e.Graphics, Width, HDR, "Update Available", 14);
            var lv = new Label { Text = "v" + Updater.CurrentVersion + "  \u2192  v" + info.Version, Font = new Font("Segoe UI Variable Text", 9F), ForeColor = Form1.C_Blue, AutoSize = true, Location = new Point(pad, HDR + 16), BackColor = Color.Transparent };
            var ll = new Label { Text = string.IsNullOrWhiteSpace(info.Changelog) ? "A new version of AVEraser is ready to install." : info.Changelog, Font = new Font("Segoe UI Variable Text", 8.5F), ForeColor = Form1.C_Text1, AutoSize = false, Location = new Point(pad, HDR + 40), Size = new Size(W - pad * 2, 52), BackColor = Color.Transparent };
            _bar = new ProgressBar { Location = new Point(pad, 170), Size = new Size(W - pad * 2, 5), Visible = false };
            _lbl = new Label { Text = "", Font = new Font("Segoe UI Variable Text", 8F), ForeColor = Form1.C_Text2, AutoSize = false, Location = new Point(pad, 179), Size = new Size(W - pad * 2, 20), BackColor = Color.Transparent, Visible = false };
            _btnU = new RoundBtn { Text = "Install Update", Location = new Point(W - pad - 248, 202), Size = new Size(120, 36), NormalColor = Form1.C_Blue, HoverColor = Form1.C_BlueH, ForeColor = Color.White, CornerRadius = 10, Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Bold), Cursor = Cursors.Default };
            _btnU.Click += async (s, e) => await Go();
            _btnS = new RoundBtn { Text = "Skip for Now", Location = new Point(W - pad - 118, 202), Size = new Size(118, 36), NormalColor = Form1.C_Bg2, HoverColor = Form1.C_Bg3, ForeColor = Form1.C_Text1, CornerRadius = 10, Font = new Font("Segoe UI Variable Text", 9F), Cursor = Cursors.Default };
            _btnS.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.AddRange(new Control[] { lv, ll, _bar, _lbl, _btnU, _btnS });
            DialogHelper.EnableDrag(this);
        }
        private async Task Go()
        {
            _btnU.Enabled = false; _btnS.Enabled = false; _bar.Visible = true; _lbl.Visible = true; _lbl.Text = "Downloading\u2026";
            try { await Updater.DownloadAndInstallAsync(_info, pct => Invoke(new Action(() => { _bar.Value = pct; _lbl.Text = "Downloading\u2026  " + pct + "%"; }))); }
            catch (Exception ex) { _bar.Visible = false; _lbl.Visible = false; _btnU.Enabled = true; _btnS.Enabled = true; MessageBox.Show("Download failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    public class AVEntry { public string DisplayName; public string[] Processes, Services, RegistryKeys, Folders; public BundledApp[] BundledApps = new BundledApp[0]; }
    public class BundledApp { public string DisplayName; public string[] Processes, Services, RegistryKeys, Folders; }
    public class ScanResult { public string AVKey, DisplayName; public readonly List<string> FoundProcesses = new List<string>(), FoundServices = new List<string>(), FoundRegistryKeys = new List<string>(), FoundFolders = new List<string>(); public readonly Dictionary<string, BundledScanResult> FoundBundled = new Dictionary<string, BundledScanResult>(); public bool HasAnyFindings => FoundProcesses.Count > 0 || FoundServices.Count > 0 || FoundRegistryKeys.Count > 0 || FoundFolders.Count > 0 || FoundBundled.Count > 0; }
    public class BundledScanResult { public string DisplayName; public readonly List<string> Processes = new List<string>(), Services = new List<string>(), RegistryKeys = new List<string>(), Folders = new List<string>(); public bool HasAny => Processes.Count > 0 || Services.Count > 0 || RegistryKeys.Count > 0 || Folders.Count > 0; }

    internal static class Gfx
    {
        internal static GraphicsPath RoundRect(Rectangle r, int rad) { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
        internal static void FillRound(Graphics g, Brush b, Rectangle r, int rad) { using (var p = RoundRect(r, rad)) g.FillPath(b, p); }
        internal static void DrawRound(Graphics g, Pen pen, Rectangle r, int rad) { using (var p = RoundRect(r, rad)) g.DrawPath(pen, p); }
    }

    internal static class DialogHelper
    {
        private static Icon _cachedIcon;

        internal static void ApplyAppIcon(Form form)
        {
            if (_cachedIcon == null)
            {
                try { _cachedIcon = new Icon(Application.ExecutablePath, 32, 32); }
                catch { try { _cachedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { _cachedIcon = null; } }
            }
            if (_cachedIcon != null) form.Icon = _cachedIcon;
        }

        internal static void PaintHeader(Graphics g, int w, int hdrH, string title, int cr)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var grad = new LinearGradientBrush(new Rectangle(0, 0, w, hdrH + cr), Form1.C_Blue, Form1.C_BlueH, LinearGradientMode.Horizontal))
                Gfx.FillRound(g, grad, new Rectangle(0, 0, w, hdrH + cr), cr);
            using (var b = new SolidBrush(Form1.C_Blue)) g.FillRectangle(b, 0, hdrH - cr, w, cr * 2);
            using (var b = new SolidBrush(Form1.C_Bg1)) g.FillRectangle(b, 0, hdrH, w, cr);
            using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
            using (var b = new SolidBrush(Color.White))
                g.DrawString(title, new Font("Segoe UI Variable Display", 11F, FontStyle.Bold), b, new RectangleF(20, 0, w - 40, hdrH), sf);
        }
        internal static void EnableDrag(Form form)
        {
            bool drag = false; Point ds = Point.Empty;
            form.MouseDown += (s, e) => { var me = (MouseEventArgs)e; if (me.Button == MouseButtons.Left) { drag = true; ds = me.Location; } };
            form.MouseMove += (s, e) => { var me = (MouseEventArgs)e; if (drag) { var sc = form.PointToScreen(me.Location); form.Location = new Point(sc.X - ds.X, sc.Y - ds.Y); } };
            form.MouseUp += (s, e) => drag = false;
        }
    }

    public class RoundBtn : Control
    {
        public Color NormalColor { get; set; } = Color.FromArgb(47, 119, 243);
        public Color HoverColor { get; set; } = Color.FromArgb(72, 140, 255);
        public Color OutlineColor { get; set; } = Color.FromArgb(47, 119, 243);
        public int CornerRadius { get; set; } = 10;
        public bool Outlined { get; set; } = false;
        public Image ButtonIcon { get; set; } = null;
        public int IconSize { get; set; } = 16;
        public bool IconLeft { get; set; } = true;
        private bool _hover;
        public RoundBtn()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.StandardClick |
                     ControlStyles.StandardDoubleClick,
                     true);
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
            BackColor = Color.Transparent;
            MouseEnter += (s, e) => { _hover = true; Invalidate(); };
            MouseLeave += (s, e) => { _hover = false; Invalidate(); };
            MouseUp += (s, e) => { if (((MouseEventArgs)e).Button == MouseButtons.Left && Enabled) OnClick(e); };
            EnabledChanged += (s, e) => Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Outlined)
            {
                using (var b = new SolidBrush(_hover && Enabled ? Color.FromArgb(30, OutlineColor) : Color.Transparent)) Gfx.FillRound(g, b, r, CornerRadius);
                using (var pen = new Pen(Enabled ? OutlineColor : Color.FromArgb(60, OutlineColor), 1.5f)) Gfx.DrawRound(g, pen, r, CornerRadius);
            }
            else
            {
                Color fill = !Enabled ? Color.FromArgb(NormalColor.A, NormalColor.R / 3, NormalColor.G / 3, NormalColor.B / 3) : _hover ? HoverColor : NormalColor;
                using (var b = new SolidBrush(fill)) Gfx.FillRound(g, b, r, CornerRadius);
            }
            Color tc = Enabled ? ForeColor : Color.FromArgb(90, ForeColor);
            if (ButtonIcon != null)
            {
                int gap = 6; float tw = g.MeasureString(Text, Font).Width;
                int tot = IconSize + gap + (int)tw, sx = (Width - tot) / 2, iy = (Height - IconSize) / 2;
                if (IconLeft) { g.DrawImage(ButtonIcon, sx, iy, IconSize, IconSize); using (var sf = new StringFormat { LineAlignment = StringAlignment.Center }) using (var b = new SolidBrush(tc)) g.DrawString(Text, Font, b, new RectangleF(sx + IconSize + gap, 0, tw + 2, Height), sf); }
                else { using (var sf = new StringFormat { LineAlignment = StringAlignment.Center }) using (var b = new SolidBrush(tc)) g.DrawString(Text, Font, b, new RectangleF(sx, 0, tw + 2, Height), sf); g.DrawImage(ButtonIcon, sx + (int)tw + gap, iy, IconSize, IconSize); }
            }
            else
            {
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var b = new SolidBrush(tc)) g.DrawString(Text, Font, b, new RectangleF(0, 0, Width, Height), sf);
            }
        }
    }

    public class ProgressBar : Control
    {
        public Color BarColor { get; set; } = Color.FromArgb(47, 119, 243);
        public Color TrackColor { get; set; } = Color.FromArgb(13, 15, 18);
        private int _target; private float _vis;
        private readonly System.Windows.Forms.Timer _t = new System.Windows.Forms.Timer { Interval = 16 };
        public int Value { get { return _target; } set { _target = Math.Max(0, Math.Min(100, value)); _t.Start(); } }
        public ProgressBar()
        {
            DoubleBuffered = true; Height = 5;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _t.Tick += (s, e) => { _vis += (_target - _vis) * 0.10f; Invalidate(); if (Math.Abs(_vis - _target) < 0.2f) { _vis = _target; _t.Stop(); Invalidate(); } };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(TrackColor)) Gfx.FillRound(g, b, new Rectangle(0, 0, Width, Height), 2);
            int w = (int)(Width * _vis / 100f);
            if (w > 4) using (var b = new SolidBrush(BarColor)) Gfx.FillRound(g, b, new Rectangle(0, 0, w, Height), 2);
        }
    }

    public class AVGrid : Control
    {
        public event EventHandler CheckChanged;
        private class Row { public string Key, Name; public bool Checked; public int Total, Procs, Svcs, Regs, Flds; public bool IsClean; }
        private readonly List<Row> _rows = new List<Row>(); private int _hover = -1;
        private const int HDR_H = 36, ROW_H = 52;
        public AVGrid()
        {
            DoubleBuffered = true; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            MouseMove += (s, e) => { int i = Hit(e.Y); if (i != _hover) { _hover = i; Invalidate(); } };
            MouseLeave += (s, e) => { _hover = -1; Invalidate(); };
            MouseClick += (s, e) => { int i = Hit(e.Y); if (i >= 0 && !_rows[i].IsClean) { _rows[i].Checked = !_rows[i].Checked; CheckChanged?.Invoke(this, EventArgs.Empty); Invalidate(); } };
        }
        public void ClearItems() { _rows.Clear(); Invalidate(); }
        public void ShowClean() { _rows.Clear(); _rows.Add(new Row { IsClean = true }); Invalidate(); }
        public void SetAllChecked(bool v) { foreach (var r in _rows) if (!r.IsClean) r.Checked = v; CheckChanged?.Invoke(this, EventArgs.Empty); Invalidate(); }
        public List<string> GetCheckedKeys() { var l = new List<string>(); foreach (var r in _rows) if (r.Checked) l.Add(r.Key); return l; }
        public void AddRow(string key, string name, int total, int p, int s, int reg, int f) { _rows.Add(new Row { Key = key, Name = name, Total = total, Procs = p, Svcs = s, Regs = reg, Flds = f }); Invalidate(); }
        private int Hit(int y) { int i = (y - HDR_H) / ROW_H; return (y >= HDR_H && i >= 0 && i < _rows.Count) ? i : -1; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var b = new SolidBrush(Form1.C_Bg0)) g.FillRectangle(b, 0, 0, Width, HDR_H);
            using (var p = new Pen(Form1.C_Line)) g.DrawLine(p, 0, HDR_H - 1, Width, HDR_H - 1);

            using (var hf = new Font("Segoe UI Variable Text", 7.5F, FontStyle.Bold))
            using (var hb = new SolidBrush(Form1.C_Text2))
            using (var sL = new StringFormat { LineAlignment = StringAlignment.Center })
            using (var sR = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far })
            {
                g.DrawString("PRODUCT", hf, hb, new RectangleF(50, 0, 220, HDR_H), sL);
                g.DrawString("RESIDUES", hf, hb, new RectangleF(280, 0, 280, HDR_H), sL);
                g.DrawString("TOTAL", hf, hb, new RectangleF(Width - 140, 0, 110, HDR_H), sR);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i]; int y = HDR_H + i * ROW_H;
                var rect = new Rectangle(0, y, Width, ROW_H);
                Color bg = row.Checked ? Color.FromArgb(22, 47, 119, 243) : i == _hover ? Form1.C_Bg3 : i % 2 == 0 ? Form1.C_Bg1 : Form1.C_Bg2;
                using (var b = new SolidBrush(bg)) g.FillRectangle(b, rect);
                using (var p = new Pen(Form1.C_Line)) g.DrawLine(p, 16, rect.Bottom - 1, rect.Width - 16, rect.Bottom - 1);

                if (row.IsClean)
                {
                    using (var b = new SolidBrush(Form1.C_Text1))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString("No residues found", new Font("Segoe UI Variable Text", 9.5F), b, new RectangleF(0, y, Width, ROW_H), sf);
                    continue;
                }

                var cb = new Rectangle(16, y + (ROW_H - 18) / 2, 18, 18);
                using (var path = Gfx.RoundRect(cb, 5))
                {
                    if (row.Checked)
                    {
                        using (var b = new SolidBrush(Form1.C_Blue)) g.FillPath(b, path);
                        using (var p = new Pen(Color.White, 2f))
                        { g.DrawLine(p, cb.Left + 4, cb.Top + 9, cb.Left + 8, cb.Bottom - 5); g.DrawLine(p, cb.Left + 8, cb.Bottom - 5, cb.Right - 3, cb.Top + 4); }
                    }
                    else using (var p = new Pen(Form1.C_Line, 1.5f)) g.DrawPath(p, path);
                }

                using (var b = new SolidBrush(Form1.C_Text0))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                    g.DrawString(row.Name, new Font("Segoe UI Variable Text", 9.5F), b, new RectangleF(50, y, 225, ROW_H), sf);

                var parts = new List<string>();
                if (row.Procs > 0) parts.Add(row.Procs + " Process" + (row.Procs != 1 ? "es" : ""));
                if (row.Svcs > 0) parts.Add(row.Svcs + " Service" + (row.Svcs != 1 ? "s" : ""));
                if (row.Regs > 0) parts.Add(row.Regs + " Registry" + (row.Regs != 1 ? " Keys" : " Key"));
                if (row.Flds > 0) parts.Add(row.Flds + " Folder" + (row.Flds != 1 ? "s" : ""));
                using (var b = new SolidBrush(Form1.C_Text1))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
                    g.DrawString(string.Join("   ", parts), new Font("Segoe UI Variable Text", 8.5F), b, new RectangleF(284, y, Width - 284 - 140, ROW_H), sf);

                using (var b = new SolidBrush(Form1.C_Text1))
                using (var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                    g.DrawString(row.Total.ToString(), new Font("Segoe UI Variable Text", 11F, FontStyle.Bold), b, new RectangleF(Width - 140, y, 110, ROW_H), sf);
            }
        }
    }

    public class Dialog : Form
    {
        public Dialog(string title, string body, string okLabel, string cancelLabel)
        {
            const int W = 390, pad = 22, btnH = 36, btnW = 100, accentH = 4;
            Text = "AVEraser"; FormBorderStyle = FormBorderStyle.None; BackColor = Form1.C_Bg1;
            StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
            Form1.SetRoundedRegion(this, 12);
            int bh; using (var tmp = Graphics.FromHwnd(IntPtr.Zero)) bh = (int)tmp.MeasureString(body, new Font("Segoe UI Variable Text", 9F), W - pad * 2).Height + 8;
            bh = Math.Max(bh, 36);
            int titleY = accentH + 16, bodyY = titleY + 30, btnY = bodyY + bh + 18;
            ClientSize = new Size(W, btnY + btnH + pad); Form1.SetRoundedRegion(this, 12);
            Paint += (s, e) => { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; using (var b = new SolidBrush(Form1.C_Blue)) g.FillRectangle(b, 0, 0, Width, accentH); using (var pen = new Pen(Form1.C_Line, 1f)) Gfx.DrawRound(g, pen, new Rectangle(0, 0, Width - 1, Height - 1), 12); };
            var lt = new Label { Text = title, Font = new Font("Segoe UI Variable Display", 11F, FontStyle.Bold), ForeColor = Form1.C_Text0, AutoSize = true, Location = new Point(pad, titleY), BackColor = Color.Transparent };
            var lb = new Label { Text = body, Font = new Font("Segoe UI Variable Text", 9F), ForeColor = Form1.C_Text1, AutoSize = false, Location = new Point(pad, bodyY), Size = new Size(W - pad * 2, bh), BackColor = Color.Transparent };
            var bOK = new RoundBtn { Text = okLabel, Location = new Point(W - pad - btnW * 2 - 8, btnY), Size = new Size(btnW, btnH), NormalColor = Form1.C_Blue, HoverColor = Form1.C_BlueH, ForeColor = Color.White, CornerRadius = 10, Font = new Font("Segoe UI Variable Text", 9.5F, FontStyle.Bold), Cursor = Cursors.Default };
            var bC = new RoundBtn { Text = cancelLabel, Location = new Point(W - pad - btnW, btnY), Size = new Size(btnW, btnH), NormalColor = Form1.C_Bg2, HoverColor = Form1.C_Bg3, ForeColor = Form1.C_Text1, CornerRadius = 10, Font = new Font("Segoe UI Variable Text", 9.5F), Cursor = Cursors.Default };
            bOK.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            bC.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.AddRange(new Control[] { lt, lb, bOK, bC });
            DialogHelper.EnableDrag(this);
        }
    }
}