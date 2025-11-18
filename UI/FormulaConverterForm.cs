using System;
using System.Drawing;
using System.Windows.Forms;
using CrystalToSSRS.Converters;
using CrystalToSSRS.Models;

namespace CrystalToSSRS.UI
{
    public class FormulaConverterForm : Form
    {
        private ReportFormula _formula;
        private FormulaToExpressionConverter _converter;
        
        private TextBox txtCrystalFormula;
        private TextBox txtSSRSExpression;
        private Button btnConvert;
        private Button btnCopy;
        private Button btnSave;
        private Button btnClose;
        private Label lblStatus;
        private SplitContainer splitContainer;
        
        public FormulaConverterForm(ReportFormula formula, FormulaToExpressionConverter converter)
        {
            _formula = formula;
            _converter = converter;
            
            InitializeComponent();
            LoadFormula();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Formül Dönüştürücü - Crystal Reports → SSRS";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Ana konteyner
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            
            // SplitContainer
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 250
            };
            
            // Crystal Formula Panel
            var crystalPanel = new Panel { Dock = DockStyle.Fill };
            var lblCrystal = new Label
            {
                Text = "Crystal Reports Formülü:",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(5)
            };
            txtCrystalFormula = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10F),
                BackColor = Color.FromArgb(255, 255, 230)
            };
            crystalPanel.Controls.Add(txtCrystalFormula);
            crystalPanel.Controls.Add(lblCrystal);
            txtCrystalFormula.BringToFront();
            
            // SSRS Expression Panel
            var ssrsPanel = new Panel { Dock = DockStyle.Fill };
            var lblSSRS = new Label
            {
                Text = "SSRS Expression:",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(5)
            };
            txtSSRSExpression = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10F),
                BackColor = Color.FromArgb(230, 255, 230),
                ReadOnly = true
            };
            ssrsPanel.Controls.Add(txtSSRSExpression);
            ssrsPanel.Controls.Add(lblSSRS);
            txtSSRSExpression.BringToFront();
            
            splitContainer.Panel1.Controls.Add(crystalPanel);
            splitContainer.Panel2.Controls.Add(ssrsPanel);
            
            // Butonlar
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };
            
            btnConvert = new Button
            {
                Text = "Dönüştür",
                Width = 100,
                Height = 30,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConvert.Click += OnConvert;
            
            btnCopy = new Button
            {
                Text = "SSRS'i Kopyala",
                Width = 120,
                Height = 30
            };
            btnCopy.Click += OnCopy;
            
            btnSave = new Button
            {
                Text = "Kaydet",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnSave.Click += OnSave;
            
            btnClose = new Button
            {
                Text = "Kapat",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };
            
            buttonPanel.Controls.AddRange(new Control[] { btnConvert, btnCopy, btnSave, btnClose });
            
            // Status label
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F),
                Padding = new Padding(5, 0, 0, 0)
            };
            
            mainPanel.Controls.Add(splitContainer, 0, 0);
            mainPanel.Controls.Add(buttonPanel, 0, 1);
            mainPanel.Controls.Add(lblStatus, 0, 2);
            
            this.Controls.Add(mainPanel);
            this.AcceptButton = btnConvert;
            this.CancelButton = btnClose;
        }
        
        private void LoadFormula()
        {
            txtCrystalFormula.Text = _formula.FormulaText;
            lblStatus.Text = $"Formül: {_formula.Name} ({_formula.DataType})";
            OnConvert(null, null);
        }
        
        private void OnConvert(object sender, EventArgs e)
        {
            try
            {
                var ssrsExpression = _converter.ConvertFormula(txtCrystalFormula.Text);
                txtSSRSExpression.Text = ssrsExpression;
                lblStatus.Text = "✓ Dönüştürme başarılı!";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                txtSSRSExpression.Text = $"HATA:\n{ex.Message}\n\n{ex.StackTrace}";
                lblStatus.Text = "✗ Dönüştürme hatası!";
                lblStatus.ForeColor = Color.Red;
            }
        }
        
        private void OnCopy(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSSRSExpression.Text))
            {
                Clipboard.SetText(txtSSRSExpression.Text);
                lblStatus.Text = "✓ SSRS expression panoya kopyalandı!";
                lblStatus.ForeColor = Color.Blue;
            }
        }
        
        private void OnSave(object sender, EventArgs e)
        {
            _formula.FormulaText = txtCrystalFormula.Text;
            lblStatus.Text = "✓ Kaydedildi!";
        }
    }
}
