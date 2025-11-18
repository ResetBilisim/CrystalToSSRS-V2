using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CrystalToSSRS.Converters;
using CrystalToSSRS.Models;

namespace CrystalToSSRS.UI
{
    public class BatchFormulaConverterForm : Form
    {
        private List<ReportFormula> _formulas;
        private FormulaToExpressionConverter _converter;
        
        private DataGridView dgvFormulas;
        private Button btnConvertAll;
        private Button btnExport;
        private Button btnClose;
        private ProgressBar progressBar;
        private Label lblStatus;
        
        public BatchFormulaConverterForm(List<ReportFormula> formulas, FormulaToExpressionConverter converter)
        {
            _formulas = formulas;
            _converter = converter;
            
            InitializeComponent();
            LoadFormulas();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Toplu Formül Dönüştürücü";
            this.Size = new Size(1200, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            
            // DataGridView
            dgvFormulas = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9F)
            };
            
            dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Formül Adı",
                DataPropertyName = "Name",
                Width = 150,
                ReadOnly = true
            });
            
            dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DataType",
                HeaderText = "Tip",
                DataPropertyName = "DataType",
                Width = 100,
                ReadOnly = true
            });
            
            dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CrystalFormula",
                HeaderText = "Crystal Formül",
                DataPropertyName = "FormulaText",
                Width = 300,
                ReadOnly = true
            });
            
            dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SSRSExpression",
                HeaderText = "SSRS Expression",
                Width = 300,
                ReadOnly = true
            });
            
            dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Durum",
                Width = 100,
                ReadOnly = true
            });
            
            // Progress bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous
            };
            
            // Butonlar
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };
            
            btnConvertAll = new Button
            {
                Text = "Tümünü Dönüştür",
                Width = 130,
                Height = 30,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConvertAll.Click += OnConvertAll;
            
            btnExport = new Button
            {
                Text = "Excel'e Aktar",
                Width = 120,
                Height = 30
            };
            btnExport.Click += OnExport;
            
            btnClose = new Button
            {
                Text = "Kapat",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            
            buttonPanel.Controls.AddRange(new Control[] { btnConvertAll, btnExport, btnClose });
            
            // Status
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F)
            };
            
            mainPanel.Controls.Add(dgvFormulas, 0, 0);
            mainPanel.Controls.Add(progressBar, 0, 1);
            mainPanel.Controls.Add(buttonPanel, 0, 2);
            mainPanel.Controls.Add(lblStatus, 0, 3);
            
            this.Controls.Add(mainPanel);
        }
        
        private void LoadFormulas()
        {
            dgvFormulas.Rows.Clear();
            
            foreach (var formula in _formulas)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dgvFormulas);
                row.Cells[0].Value = formula.Name;
                row.Cells[1].Value = formula.DataType;
                row.Cells[2].Value = formula.FormulaText;
                row.Cells[3].Value = "";
                row.Cells[4].Value = "Bekliyor";
                row.Cells[4].Style.BackColor = Color.LightGray;
                
                dgvFormulas.Rows.Add(row);
            }
            
            lblStatus.Text = $"Toplam {_formulas.Count} formül yüklendi";
        }
        
        private void OnConvertAll(object sender, EventArgs e)
        {
            progressBar.Maximum = _formulas.Count;
            progressBar.Value = 0;
            
            int successCount = 0;
            int errorCount = 0;
            
            for (int i = 0; i < _formulas.Count; i++)
            {
                var formula = _formulas[i];
                var row = dgvFormulas.Rows[i];
                
                try
                {
                    var ssrsExpression = _converter.ConvertFormula(formula.FormulaText);
                    row.Cells[3].Value = ssrsExpression;
                    row.Cells[4].Value = "✓ Başarılı";
                    row.Cells[4].Style.BackColor = Color.LightGreen;
                    successCount++;
                }
                catch (Exception ex)
                {
                    row.Cells[3].Value = $"HATA: {ex.Message}";
                    row.Cells[4].Value = "✗ Hata";
                    row.Cells[4].Style.BackColor = Color.LightCoral;
                    errorCount++;
                }
                
                progressBar.Value = i + 1;
                lblStatus.Text = $"İşleniyor... {i + 1}/{_formulas.Count}";
                Application.DoEvents();
            }
            
            lblStatus.Text = $"Tamamlandı! Başarılı: {successCount}, Hatalı: {errorCount}";
            lblStatus.ForeColor = errorCount > 0 ? Color.Orange : Color.Green;
            
            MessageBox.Show($"Dönüştürme tamamlandı!\n\n" +
                $"✓ Başarılı: {successCount}\n" +
                $"✗ Hatalı: {errorCount}",
                "Sonuç", MessageBoxButtons.OK, 
                errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        
        private void OnExport(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Files (*.csv)|*.csv";
                sfd.Title = "Formülleri Dışa Aktar";
                
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ExportToCsv(sfd.FileName);
                }
            }
        }
        
        private void ExportToCsv(string fileName)
        {
            try
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Formül Adı,Veri Tipi,Crystal Formül,SSRS Expression,Durum");
                
                foreach (DataGridViewRow row in dgvFormulas.Rows)
                {
                    var line = string.Join(",", 
                        EscapeCsv(row.Cells[0].Value?.ToString()),
                        EscapeCsv(row.Cells[1].Value?.ToString()),
                        EscapeCsv(row.Cells[2].Value?.ToString()),
                        EscapeCsv(row.Cells[3].Value?.ToString()),
                        EscapeCsv(row.Cells[4].Value?.ToString())
                    );
                    csv.AppendLine(line);
                }
                
                System.IO.File.WriteAllText(fileName, csv.ToString(), System.Text.Encoding.UTF8);
                lblStatus.Text = $"✓ Dışa aktarıldı: {fileName}";
                MessageBox.Show("Başarıyla dışa aktarıldı!", "Başarılı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dışa aktarma hatası:\n{ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            
            return value;
        }
    }
}
