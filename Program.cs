using System;
using System.Windows.Forms;
using CrystalToSSRS.UI;

namespace CrystalToSSRS
{
    static class Program
    {
        /// <summary>
        /// Uygulamanın ana giriş noktası.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Splash screen göster (opsiyonel)
            ShowSplashScreen();
            
            Application.Run(new MainForm());
        }
        
        private static void ShowSplashScreen()
        {
            var splash = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new System.Drawing.Size(400, 200),
                BackColor = System.Drawing.Color.FromArgb(0, 122, 204)
            };
            
            var label = new Label
            {
                Text = "Crystal Reports to SSRS\nConverter\n\nYükleniyor...",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold)
            };
            
            splash.Controls.Add(label);
            splash.Show();
            Application.DoEvents();
            
            System.Threading.Thread.Sleep(1500);
            splash.Close();
        }
    }
}
