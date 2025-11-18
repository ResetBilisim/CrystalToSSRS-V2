using System;
using System.Drawing;
using System.Windows.Forms;
using CrystalToSSRS.Models;

namespace CrystalToSSRS.UI
{
    public class DataSourceSettingsForm : Form
    {
        private OracleConnectionInfo _connectionInfo;
        
        private TextBox txtServerName;
        private NumericUpDown numPort;
        private TextBox txtServiceName;
        private TextBox txtUserId;
        private TextBox txtPassword;
        private Button btnTest;
        private Button btnSave;
        private Button btnCancel;
        private Label lblStatus;
        
        public DataSourceSettingsForm(OracleConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
            InitializeComponent();
            LoadConnectionInfo();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Oracle Veri Kaynağı Ayarları";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(20)
            };
            
            // Labels and controls
            mainPanel.Controls.Add(new Label { Text = "Server:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            txtServerName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            mainPanel.Controls.Add(txtServerName, 1, 0);
            
            mainPanel.Controls.Add(new Label { Text = "Port:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            numPort = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 65535, Value = 1521, Font = new Font("Segoe UI", 10F) };
            mainPanel.Controls.Add(numPort, 1, 1);
            
            mainPanel.Controls.Add(new Label { Text = "Service Name:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            txtServiceName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            mainPanel.Controls.Add(txtServiceName, 1, 2);
            
            mainPanel.Controls.Add(new Label { Text = "User ID:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            txtUserId = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            mainPanel.Controls.Add(txtUserId, 1, 3);
            
            mainPanel.Controls.Add(new Label { Text = "Password:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 4);
            txtPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Font = new Font("Segoe UI", 10F) };
            mainPanel.Controls.Add(txtPassword, 1, 4);
            
            // Butonlar
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };
            
            btnTest = new Button { Text = "Bağlantıyı Test Et", Width = 130, Height = 30 };
            btnTest.Click += OnTestConnection;
            
            btnSave = new Button { Text = "Kaydet", Width = 100, Height = 30, DialogResult = DialogResult.OK };
            btnSave.Click += OnSave;
            
            btnCancel = new Button { Text = "İptal", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            
            buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnSave, btnTest });
            mainPanel.Controls.Add(buttonPanel, 1, 5);
            
            // Status
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F)
            };
            mainPanel.Controls.Add(lblStatus, 0, 6);
            mainPanel.SetColumnSpan(lblStatus, 2);
            
            this.Controls.Add(mainPanel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }
        
        private void LoadConnectionInfo()
        {
            txtServerName.Text = _connectionInfo.ServerName;
            numPort.Value = _connectionInfo.Port;
            txtServiceName.Text = _connectionInfo.ServiceName;
            txtUserId.Text = _connectionInfo.UserId;
            txtPassword.Text = _connectionInfo.Password;
        }
        
        private void OnTestConnection(object sender, EventArgs e)
        {
            lblStatus.Text = "Bağlantı test ediliyor...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();
            
            try
            {
                // Simüle edilmiş test
                System.Threading.Thread.Sleep(1000);
                lblStatus.Text = "✓ Bağlantı parametreleri doğru görünüyor! (Test simülasyonu)";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"✗ Bağlantı hatası: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }
        
        private void OnSave(object sender, EventArgs e)
        {
            _connectionInfo.ServerName = txtServerName.Text;
            _connectionInfo.Port = (int)numPort.Value;
            _connectionInfo.ServiceName = txtServiceName.Text;
            _connectionInfo.UserId = txtUserId.Text;
            _connectionInfo.Password = txtPassword.Text;
        }
    }
}
