using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AVEraser
{
    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int n);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string cls, string title);

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool pendingDelete = false, background = false;
            foreach (string a in args)
            {
                if (a == "--delete") pendingDelete = true;
                if (a == "--background") background = true;
            }

            bool created;
            using (var mutex = new Mutex(true, "AVEraser_SingleInstance", out created))
            {
                if (!created)
                {
                    var h = FindWindow(null, "AVEraser");
                    if (h != IntPtr.Zero) { ShowWindow(h, 9); SetForegroundWindow(h); }
                    return;
                }
                Application.Run(new Form1(pendingDelete, background));
            }
        }
    }
}