using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace CrystalToSSRS.UI
{
    public class RdlPreviewForm : Form
    {
        private string _rdlContent;
        private RichTextBox txtRdl;
        private Button btnSave;
        private Button btnCopy;
        private Button btnValidate;
        private Button btnClose;
        private Label lblStatus;
        private ListBox lstFields;
        
        public RdlPreviewForm(string rdlContent)
        {
            _rdlContent = rdlContent;
            InitializeComponent();
            LoadRdl();
        }
        
        private void InitializeComponent()
        {
            this.Text = "RDL Preview and Field Details";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            
            // RichTextBox for RDL content
            var textPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            
            var textLabel = new Label
            {
                Text = "RDL Content",
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.LightGray,
                Padding = new Padding(5, 0, 0, 0)
            };
            textPanel.Controls.Add(textLabel);
            
            txtRdl = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                WordWrap = false,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            
            textPanel.Controls.Add(txtRdl);
            txtRdl.BringToFront();
            
            // Fields panel on the right
            var fieldsPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            
            var fieldsLabel = new Label
            {
                Text = "Fields and Parameters",
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.LightGray,
                Padding = new Padding(5, 0, 0, 0)
            };
            fieldsPanel.Controls.Add(fieldsLabel);
            
            lstFields = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };
            
            fieldsPanel.Controls.Add(lstFields);
            lstFields.BringToFront();
            
            // Add controls to main panel
            mainPanel.Controls.Add(textPanel, 0, 0);
            mainPanel.SetRowSpan(textPanel, 1);
            mainPanel.SetColumnSpan(textPanel, 1);
            
            mainPanel.Controls.Add(fieldsPanel, 1, 0);
            mainPanel.SetRowSpan(fieldsPanel, 1);
            mainPanel.SetColumnSpan(fieldsPanel, 1);
            
            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };
            
            btnValidate = new Button
            {
                Text = "Validate XML",
                Width = 130,
                Height = 30
            };
            btnValidate.Click += OnValidate;
            
            btnCopy = new Button
            {
                Text = "Copy",
                Width = 100,
                Height = 30
            };
            btnCopy.Click += OnCopy;
            
            btnSave = new Button
            {
                Text = "Save As...",
                Width = 120,
                Height = 30
            };
            btnSave.Click += OnSave;
            
            btnClose = new Button
            {
                Text = "Close",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            
            buttonPanel.Controls.AddRange(new Control[] { btnValidate, btnCopy, btnSave, btnClose });
            
            // Status
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F),
                AutoSize = false
            };
            
            mainPanel.Controls.Add(buttonPanel, 0, 1);
            mainPanel.SetColumnSpan(buttonPanel, 2);
            
            mainPanel.Controls.Add(lblStatus, 0, 2);
            mainPanel.SetColumnSpan(lblStatus, 2);
            
            this.Controls.Add(mainPanel);
        }
        
        private void LoadRdl()
        {
            txtRdl.Text = _rdlContent;
            ApplySyntaxHighlighting();
            ExtractAndDisplayFields();
            lblStatus.Text = $"RDL size: {_rdlContent.Length:N0} characters";
        }
        
        private void ApplySyntaxHighlighting()
        {
            try
            {
                txtRdl.SelectAll();
                txtRdl.SelectionColor = Color.FromArgb(220, 220, 220);
                
                // XML tags - blue
                HighlightPattern(@"<[^>]+>", Color.FromArgb(86, 156, 214));
                
                // Attribute names - light blue
                HighlightPattern(@"\s\w+=""", Color.FromArgb(156, 220, 254));
                
                // String values - orange
                HighlightPattern(@"""[^""]*""", Color.FromArgb(206, 145, 120));
                
                // Comments - green
                HighlightPattern(@"<!--.*?-->", Color.FromArgb(87, 166, 74));
                
                txtRdl.Select(0, 0);
            }
            catch { }
        }
        
        private void HighlightPattern(string pattern, Color color)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(pattern);
                var matches = regex.Matches(txtRdl.Text);
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    txtRdl.Select(match.Index, match.Length);
                    txtRdl.SelectionColor = color;
                }
            }
            catch { }
        }
        
        private void ExtractAndDisplayFields()
        {
            lstFields.Items.Clear();
            
            try
            {
                var xdoc = XDocument.Parse(_rdlContent);
                
                // Extract fields from DataSets
                var ns = XNamespace.Get("http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition");
                
                lstFields.Items.Add("=== FIELDS ===");
                
                var fields = xdoc.Descendants(ns + "Field");
                foreach (var field in fields)
                {
                    var name = field.Attribute("Name")?.Value ?? "Unknown";
                    var dataType = field.Element(ns + "rd:TypeName")?.Value ?? "Unknown";
                    lstFields.Items.Add($"[FIELD] {name} ({dataType})");
                }
                
                // Extract parameters
                lstFields.Items.Add("");
                lstFields.Items.Add("=== PARAMETERS ===");
                
                var parameters = xdoc.Descendants(ns + "ReportParameter");
                foreach (var param in parameters)
                {
                    var name = param.Attribute("Name")?.Value ?? "Unknown";
                    var dataType = param.Element(ns + "DataType")?.Value ?? "Unknown";
                    var prompt = param.Element(ns + "Prompt")?.Value ?? "";
                    lstFields.Items.Add($"[PARAM] {name} ({dataType}) - {prompt}");
                }
                
                // Extract variables/formulas
                lstFields.Items.Add("");
                lstFields.Items.Add("=== VARIABLES/FORMULAS ===");
                
                var variables = xdoc.Descendants(ns + "Variable");
                foreach (var var in variables)
                {
                    var name = var.Attribute("Name")?.Value ?? "Unknown";
                    var value = var.Element(ns + "Value")?.Value ?? "";
                    lstFields.Items.Add($"[VAR] {name} = {value.Substring(0, Math.Min(50, value.Length))}...");
                }
                
                if (lstFields.Items.Count == 0)
                {
                    lstFields.Items.Add("No fields, parameters, or variables found.");
                }
            }
            catch (Exception ex)
            {
                lstFields.Items.Add($"Error parsing RDL: {ex.Message}");
            }
        }
        
        private void OnValidate(object sender, EventArgs e)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(_rdlContent);
                
                lblStatus.Text = "✓ XML is valid!";
                lblStatus.ForeColor = Color.Green;
                
                MessageBox.Show($"XML structure is valid!\n\n" +
                    $"Root: {xmlDoc.DocumentElement.Name}\n" +
                    $"Namespace: {xmlDoc.DocumentElement.NamespaceURI}",
                    "Validation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "✗ XML is invalid!";
                lblStatus.ForeColor = Color.Red;
                
                MessageBox.Show($"XML error:\n\n{ex.Message}",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OnCopy(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(_rdlContent);
                lblStatus.Text = "✓ RDL copied to clipboard!";
                lblStatus.ForeColor = Color.Blue;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"✗ Copy failed: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }
        
        private void OnSave(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "SSRS Report Files (*.rdl)|*.rdl";
                sfd.Title = "Save RDL File";
                
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // UTF-8 with BOM encoding (UTF-8 BOM: EF BB BF)
                        var encoding = new System.Text.UTF8Encoding(true);
                        System.IO.File.WriteAllText(sfd.FileName, _rdlContent, encoding);
                        
                        // Verify BOM was written
                        var fileBytes = System.IO.File.ReadAllBytes(sfd.FileName);
                        if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
                        {
                            lblStatus.Text = $"✓ Saved with UTF-8 BOM: {sfd.FileName}";
                            lblStatus.ForeColor = Color.Green;
                        }
                        else
                        {
                            lblStatus.Text = $"✗ Saved but BOM missing: {sfd.FileName}";
                            lblStatus.ForeColor = Color.Orange;
                        }
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = $"✗ Save failed: {ex.Message}";
                        lblStatus.ForeColor = Color.Red;
                    }
                }
            }
        }
    }
}
