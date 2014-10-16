using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace PlotCharts
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Process.GetProcessesByName(
                    System.IO.Path.GetFileNameWithoutExtension(
                        System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1
                )
            {
                MessageBox.Show("すでにプログラムが動作中です。", "多重起動エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PlotCharts());
        }
    }
}
