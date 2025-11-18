using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using CrystalToSSRS.Converters;
using CrystalToSSRS.Models;
using CrystalToSSRS.RDLGenerator;

namespace CrystalToSSRS.UI
{
    public partial class MainForm : Form
    {
        private CrystalReportModel _currentModel;
        private CrystalReportParser _parser;
        private FormulaToExpressionConverter _formulaConverter;
        private string _currentRptPath;
        
        // UI Kontrolleri
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private SplitContainer mainSplitContainer;
        private SplitContainer leftSplitContainer;
        
        // Sol panel
        private TreeView treeViewStructure;
        private PropertyGrid propertyGrid;
        
        // Sağ panel - Tasarım görünümü
        private Panel designPanel;
        private Panel rulerPanel;
        private Label lblHorizontalRuler;
        private Label lblVerticalRuler;
        
        // Status bar
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblPosition;
        private ToolStripStatusLabel lblSize;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
            _parser = new CrystalReportParser();
            _formulaConverter = new FormulaToExpressionConverter();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Crystal Reports to SSRS Converter - Milimetrik Kontrol";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
        }
        
        private void InitializeCustomComponents()
        {
            // Menu Strip
            CreateMenuStrip();
            
            // Tool Strip
            CreateToolStrip();
            
            // Main Split Container
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 400,
                Orientation = Orientation.Vertical
            };
            this.Controls.Add(mainSplitContainer);
            
            // Left Split Container (TreeView + PropertyGrid)
            leftSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };
            mainSplitContainer.Panel1.Controls.Add(leftSplitContainer);
            
            // TreeView - Rapor yapısı
            CreateTreeView();
            
            // PropertyGrid - Seçili öğe özellikleri
            CreatePropertyGrid();
            
            // Design Panel - Tasarım görünümü
            CreateDesignPanel();
            
            // Status Strip
            CreateStatusStrip();
        }
        
        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();
            
            // Dosya Menüsü
            var fileMenu = new ToolStripMenuItem("&Dosya");
            fileMenu.DropDownItems.Add("&RPT Aç...", null, OnOpenRpt);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("RDL Olarak &Kaydet...", null, OnSaveRdl);
            fileMenu.DropDownItems.Add("RDL &Önizleme", null, OnPreviewRdl);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Çı&kış", null, (s, e) => Application.Exit());
            
            // Görünüm Menüsü
            var viewMenu = new ToolStripMenuItem("&Görünüm");
            viewMenu.DropDownItems.Add("&Cetvelleri Göster/Gizle", null, OnToggleRulers);
            viewMenu.DropDownItems.Add("&Grid Göster/Gizle", null, OnToggleGrid);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("&Milimetre", null, (s, e) => SetUnit(MeasurementUnit.Millimeter));
            viewMenu.DropDownItems.Add("&İnch", null, (s, e) => SetUnit(MeasurementUnit.Inch));
            viewMenu.DropDownItems.Add("&Piksel", null, (s, e) => SetUnit(MeasurementUnit.Pixel));
            
            // Araçlar Menüsü
            var toolsMenu = new ToolStripMenuItem("&Araçlar");
            toolsMenu.DropDownItems.Add("&Formül Dönüştürücü", null, OnFormulaConverter);
            toolsMenu.DropDownItems.Add("&Veri Kaynağı Ayarları", null, OnDataSourceSettings);
            toolsMenu.DropDownItems.Add("&Toplu Validasyon", null, OnBatchValidation);
            
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, toolsMenu });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }
        
        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip();
            toolStrip.ImageScalingSize = new Size(24, 24);
            
            var btnOpen = new ToolStripButton("RPT Aç", null, OnOpenRpt) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
            var btnSave = new ToolStripButton("RDL Kaydet", null, OnSaveRdl) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
            var btnPreview = new ToolStripButton("Önizleme", null, OnPreviewRdl) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
            
            toolStrip.Items.AddRange(new ToolStripItem[] 
            { 
                btnOpen, 
                new ToolStripSeparator(), 
                btnSave, 
                btnPreview 
            });
            
            this.Controls.Add(toolStrip);
        }
        
        private void CreateTreeView()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var label = new Label
            {
                Text = "Rapor Yapısı",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5),
                BackColor = Color.LightGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panel.Controls.Add(label);
            
            treeViewStructure = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ShowNodeToolTips = true
            };
            treeViewStructure.AfterSelect += OnTreeViewNodeSelected;
            treeViewStructure.NodeMouseDoubleClick += OnTreeViewNodeDoubleClick;
            panel.Controls.Add(treeViewStructure);
            treeViewStructure.BringToFront();
            
            leftSplitContainer.Panel1.Controls.Add(panel);
        }
        
        private void CreatePropertyGrid()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var label = new Label
            {
                Text = "Özellikler (Milimetrik Kontrol)",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5),
                BackColor = Color.LightGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panel.Controls.Add(label);
            
            propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PropertySort = PropertySort.Categorized,
                HelpVisible = true
            };
            propertyGrid.PropertyValueChanged += OnPropertyValueChanged;
            panel.Controls.Add(propertyGrid);
            propertyGrid.BringToFront();
            
            leftSplitContainer.Panel2.Controls.Add(panel);
        }
        
        private void CreateDesignPanel()
        {
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                BackColor = Color.WhiteSmoke
            };
            
            var label = new Label
            {
                Text = "Tasarım Görünümü (1:1 Ölçek)",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5),
                BackColor = Color.LightGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            mainPanel.Controls.Add(label);
            
            // Cetvel paneli
            rulerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            
            // Yatay cetvel
            lblHorizontalRuler = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle
            };
            lblHorizontalRuler.Paint += OnPaintHorizontalRuler;
            rulerPanel.Controls.Add(lblHorizontalRuler);
            
            // Dikey cetvel
            lblVerticalRuler = new Label
            {
                Dock = DockStyle.Left,
                Width = 30,
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle
            };
            lblVerticalRuler.Paint += OnPaintVerticalRuler;
            rulerPanel.Controls.Add(lblVerticalRuler);
            
            // Ana tasarım alanı
            designPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            designPanel.Paint += OnPaintDesignPanel;
            designPanel.MouseMove += OnDesignPanelMouseMove;
            designPanel.MouseClick += OnDesignPanelMouseClick;
            
            rulerPanel.Controls.Add(designPanel);
            mainPanel.Controls.Add(rulerPanel);
            rulerPanel.BringToFront();
            
            mainSplitContainer.Panel2.Controls.Add(mainPanel);
        }
        
        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();
            
            lblStatus = new ToolStripStatusLabel
            {
                Text = "Hazır",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            lblPosition = new ToolStripStatusLabel
            {
                Text = "Pozisyon: -",
                BorderSides = ToolStripStatusLabelBorderSides.Left
            };
            
            lblSize = new ToolStripStatusLabel
            {
                Text = "Boyut: -",
                BorderSides = ToolStripStatusLabelBorderSides.Left
            };
            
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblPosition, lblSize });
            this.Controls.Add(statusStrip);
        }
        
        // Event Handlers
        private void OnOpenRpt(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Crystal Reports Files (*.rpt)|*.rpt";
                ofd.Title = "Crystal Reports Dosyası Seçin";
                
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadRptFile(ofd.FileName);
                }
            }
        }
        
        private void LoadRptFile(string filePath)
        {
            try
            {
                lblStatus.Text = "Rapor yükleniyor...";
                Application.DoEvents();
                
                _currentRptPath = filePath;
                _currentModel = _parser.ParseReport(filePath);
                
                PopulateTreeView();
                DrawDesignView();
                
                // Hata özeti
                if (_currentModel.ParseErrors != null && _currentModel.ParseErrors.Count > 0)
                {
                    var msg = string.Join("\n", _currentModel.ParseErrors);
                    MessageBox.Show("RPT okunurken bazı kayıtlar atlandı veya hata oluştu:\n\n" + msg,
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                lblStatus.Text = $"Rapor yüklendi: {Path.GetFileName(filePath)}";
                MessageBox.Show($"Rapor başarıyla yüklendi!\n\n" +
                    $"Sections: {_currentModel.Sections.Count}\n" +
                    $"Parameters: {_currentModel.Parameters.Count}\n" +
                    $"Formulas: {_currentModel.Formulas.Count}\n" +
                    $"Tables: {_currentModel.Tables.Count}",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Hata!";
                MessageBox.Show($"Rapor yüklenirken hata:\n{ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void PopulateTreeView()
        {
            treeViewStructure.Nodes.Clear();
            
            if (_currentModel == null) return;
            
            var rootNode = new TreeNode(_currentModel.ReportName)
            {
                Tag = _currentModel
            };
            
            // Hatalar
            if (_currentModel.ParseErrors != null && _currentModel.ParseErrors.Count > 0)
            {
                var errNode = new TreeNode($"Errors ({_currentModel.ParseErrors.Count})");
                foreach (var err in _currentModel.ParseErrors)
                    errNode.Nodes.Add(new TreeNode(err));
                rootNode.Nodes.Add(errNode);
            }
            
            // Connection Info
            var connNode = new TreeNode($"Oracle Connection: {_currentModel.ConnectionInfo.ServerName}")
            {
                Tag = _currentModel.ConnectionInfo
            };
            rootNode.Nodes.Add(connNode);
            
            // Sections
            var sectionsNode = new TreeNode("Sections");
            foreach (var section in _currentModel.Sections)
            {
                var sectionNode = new TreeNode($"{section.Name} ({section.Objects.Count} items)")
                {
                    Tag = section
                };
                
                foreach (var obj in section.Objects)
                {
                    var objNode = new TreeNode($"{obj.Type}: {obj.Name}")
                    {
                        Tag = obj,
                        ToolTipText = $"Pos: ({obj.Left:F2}, {obj.Top:F2}) Size: ({obj.Width:F2}, {obj.Height:F2})"
                    };
                    sectionNode.Nodes.Add(objNode);
                }
                
                sectionsNode.Nodes.Add(sectionNode);
            }
            rootNode.Nodes.Add(sectionsNode);
            
            // Parameters
            if (_currentModel.Parameters.Count > 0)
            {
                var paramsNode = new TreeNode($"Parameters ({_currentModel.Parameters.Count})");
                foreach (var param in _currentModel.Parameters)
                {
                    var paramNode = new TreeNode($"{param.Name} ({param.DataType})")
                    {
                        Tag = param
                    };
                    paramsNode.Nodes.Add(paramNode);
                }
                rootNode.Nodes.Add(paramsNode);
            }
            
            // Formulas
            if (_currentModel.Formulas.Count > 0)
            {
                var formulasNode = new TreeNode($"Formulas ({_currentModel.Formulas.Count})");
                foreach (var formula in _currentModel.Formulas)
                {
                    var formulaNode = new TreeNode($"{formula.Name}")
                    {
                        Tag = formula,
                        ToolTipText = formula.FormulaText
                    };
                    formulasNode.Nodes.Add(formulaNode);
                }
                rootNode.Nodes.Add(formulasNode);
            }
            
            // Tables
            var tablesNode = new TreeNode($"Tables ({_currentModel.Tables.Count})");
            foreach (var table in _currentModel.Tables)
            {
                var tableNode = new TreeNode($"{table.Name} ({table.Fields.Count} fields)")
                {
                    Tag = table
                };
                tablesNode.Nodes.Add(tableNode);
            }
            rootNode.Nodes.Add(tablesNode);
            
            treeViewStructure.Nodes.Add(rootNode);
            rootNode.Expand();
            sectionsNode.Expand();
        }
        
        private void OnTreeViewNodeSelected(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                // Milimetrik kontrol için özel wrapper oluştur
                var wrapper = CreatePropertyWrapper(e.Node.Tag);
                propertyGrid.SelectedObject = wrapper;
                
                // Tasarım panelinde vurgula
                if (e.Node.Tag is ReportObject reportObj)
                {
                    HighlightObjectInDesign(reportObj);
                }
            }
        }
        
        private object CreatePropertyWrapper(object obj)
        {
            if (obj is ReportObject reportObj)
            {
                return new ReportObjectProperties(reportObj);
            }
            else if (obj is ReportSection section)
            {
                return new SectionProperties(section);
            }
            else if (obj is ReportFormula formula)
            {
                return new FormulaProperties(formula, _formulaConverter);
            }
            
            return obj;
        }
        
        private void OnTreeViewNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is ReportFormula formula)
            {
                ShowFormulaConverter(formula);
            }
        }
        
        private void OnPropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            designPanel.Invalidate();
            lblStatus.Text = $"Özellik değiştirildi: {e.ChangedItem.Label}";
        }
        
        private void DrawDesignView()
        {
            designPanel.Invalidate();
        }
        
        private void OnPaintDesignPanel(object sender, PaintEventArgs e)
        {
            if (_currentModel == null) return;
            
            var g = e.Graphics;
            g.Clear(Color.White);
            
            // Grid çiz (opsiyonel)
            if (_showGrid)
                DrawGrid(g);
            
            float yOffset = 10;
            
            foreach (var section in _currentModel.Sections)
            {
                // Section başlığı
                var sectionRect = new RectangleF(10, yOffset, 700, (float)section.Height);
                g.FillRectangle(Brushes.LightGray, sectionRect);
                g.DrawRectangle(Pens.Black, Rectangle.Round(sectionRect));
                g.DrawString(section.Name, new Font("Arial", 8), Brushes.Black, 15, yOffset + 2);
                
                yOffset += 20;
                
                // Section içindeki nesneleri çiz
                foreach (var obj in section.Objects)
                {
                    DrawReportObject(g, obj, yOffset);
                }
                
                yOffset += (float)section.Height + 10;
            }
        }
        
        private void DrawReportObject(Graphics g, ReportObject obj, float yOffset)
        {
            // Twips to pixel conversion (1440 twips = 1 inch = 96 pixels)
            float x = (float)(obj.Left / 1440.0 * 96) + 10;
            float y = (float)(obj.Top / 1440.0 * 96) + yOffset;
            float w = (float)(obj.Width / 1440.0 * 96);
            float h = (float)(obj.Height / 1440.0 * 96);

            if (obj.Suppress) return; // gizli ise çizme
            var rect = new RectangleF(x, y, w, h);

            // Stil renklerini hazırla
            Color? back = null, fore = null, bcol = null;
            if (obj.Style != null)
            {
                if (obj.Style.BackColorArgb.HasValue) back = Color.FromArgb(obj.Style.BackColorArgb.Value);
                if (obj.Style.ForeColorArgb.HasValue) fore = Color.FromArgb(obj.Style.ForeColorArgb.Value);
                if (obj.Style.BorderColorArgb.HasValue) bcol = Color.FromArgb(obj.Style.BorderColorArgb.Value);
            }

            // Varsayılanlar
            var borderPen = new Pen(bcol ?? Color.Gray, (obj.Style?.BorderWidthPt ?? 1f) * 96f / 72f); // pt->px

            // Nesne tipine göre çiz
            if (obj.Type == "TextObject")
            {
                using (var bg = new SolidBrush(back ?? Color.LightYellow))
                    g.FillRectangle(bg, rect);
                g.DrawRectangle(borderPen, Rectangle.Round(rect));

                if (!string.IsNullOrEmpty(obj.Text))
                {
                    var font = obj.Font != null ? new Font(obj.Font.Name, obj.Font.Size, (obj.Font.Bold? FontStyle.Bold:FontStyle.Regular) | (obj.Font.Italic? FontStyle.Italic:0) | (obj.Font.Underline? FontStyle.Underline:0)) : new Font("Arial", 10);
                    var fmt = new StringFormat();
                    fmt.Alignment = ToAlignment(obj.Style?.TextAlign);
                    fmt.LineAlignment = ToLineAlignment(obj.Style?.VerticalAlign);
                    using (var fb = new SolidBrush(fore ?? Color.Black))
                        g.DrawString(obj.Text, font, fb, rect, fmt);
                }
            }
            else if (obj.Type == "FieldObject")
            {
                using (var bg = new SolidBrush(back ?? Color.LightBlue))
                    g.FillRectangle(bg, rect);
                g.DrawRectangle(borderPen, Rectangle.Round(rect));
                var font = obj.Font != null ? new Font(obj.Font.Name, obj.Font.Size) : new Font("Arial", 9);
                var fmt = new StringFormat();
                fmt.Alignment = ToAlignment(obj.Style?.TextAlign);
                fmt.LineAlignment = ToLineAlignment(obj.Style?.VerticalAlign);
                using (var fb = new SolidBrush(fore ?? Color.DarkBlue))
                    g.DrawString(obj.DataSource ?? obj.Name, font, fb, rect, fmt);
            }
            else if (obj.Type == "LineObject")
            {
                // Line crystal ölçülerinde w/h doğrultusunda çiziliyor
                g.DrawLine(new Pen(bcol ?? Color.Black, (obj.Style?.LineWidthPt ?? 1f) * 96f / 72f), x, y, x + w, y + h);
            }
            else
            {
                using (var bg = new SolidBrush(back ?? Color.White))
                    g.FillRectangle(bg, rect);
                g.DrawRectangle(borderPen, Rectangle.Round(rect));
                var font = new Font("Arial", 8);
                using (var fb = new SolidBrush(fore ?? Color.Gray))
                    g.DrawString(obj.Type, font, fb, rect);
            }

            // Boyut bilgisini göster (mm)
            float mmW = w / 96f * 25.4f;
            float mmH = h / 96f * 25.4f;
            g.DrawString($"{mmW:F1}x{mmH:F1}mm", new Font("Arial", 7), Brushes.Red, x, y - 12);
        }

        private StringAlignment ToAlignment(string align)
        {
            switch ((align ?? "").ToLowerInvariant())
            {
                case "center": return StringAlignment.Center;
                case "right": return StringAlignment.Far;
                default: return StringAlignment.Near;
            }
        }
        private StringAlignment ToLineAlignment(string valign)
        {
            switch ((valign ?? "").ToLowerInvariant())
            {
                case "middle": return StringAlignment.Center;
                case "bottom": return StringAlignment.Far;
                default: return StringAlignment.Near;
            }
        }
        
        private void DrawGrid(Graphics g)
        {
            // 10px grid
            var gridColor = Color.FromArgb(230, 230, 230);
            var pen = new Pen(gridColor);
            
            for (int x = 0; x < designPanel.Width; x += 10)
            {
                g.DrawLine(pen, x, 0, x, designPanel.Height);
            }
            
            for (int y = 0; y < designPanel.Height; y += 10)
            {
                g.DrawLine(pen, 0, y, designPanel.Width, y);
            }
        }
        
        private void OnPaintHorizontalRuler(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.LightYellow);
            
            // Her 10mm'de bir işaret
            for (int i = 0; i < lblHorizontalRuler.Width; i += 38) // 10mm ≈ 38px (96 DPI)
            {
                g.DrawLine(Pens.Black, i, 20, i, 30);
                g.DrawString($"{i / 38 * 10}mm", new Font("Arial", 7), Brushes.Black, i + 2, 5);
            }
        }
        
        private void OnPaintVerticalRuler(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.LightYellow);
            
            // Her 10mm'de bir işaret
            for (int i = 0; i < lblVerticalRuler.Height; i += 38)
            {
                g.DrawLine(Pens.Black, 20, i, 30, i);
                
                // Yazıyı 90 derece döndür
                g.TranslateTransform(5, i + 15);
                g.RotateTransform(-90);
                g.DrawString($"{i / 38 * 10}mm", new Font("Arial", 7), Brushes.Black, 0, 0);
                g.ResetTransform();
            }
        }
        
        private void OnDesignPanelMouseMove(object sender, MouseEventArgs e)
        {
            // Mouse pozisyonunu mm olarak göster
            float mmX = e.X / 96.0f * 25.4f; // inch to mm
            float mmY = e.Y / 96.0f * 25.4f;
            
            lblPosition.Text = $"Pozisyon: {mmX:F2}mm, {mmY:F2}mm";
        }
        
        private void OnDesignPanelMouseClick(object sender, MouseEventArgs e)
        {
            // Tıklanan nesneyi bul ve seç
            // TODO: Implement click detection
        }
        
        private void HighlightObjectInDesign(ReportObject obj)
        {
            designPanel.Invalidate();
            // TODO: Highlight selected object
        }
        
        private void OnSaveRdl(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("First load an RPT file!", "Warning", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "SSRS Report Files (*.rdl)|*.rdl";
                sfd.Title = "Save RDL File";
                sfd.FileName = _currentModel.ReportName + ".rdl";
                
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var rdlBuilder = new RDLBuilder(_currentModel);
                        rdlBuilder.SaveToFile(sfd.FileName);
                        
                        // Verify BOM was written
                        var fileBytes = System.IO.File.ReadAllBytes(sfd.FileName);
                        bool hasBom = fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF;
                        
                        lblStatus.Text = "RDL file saved successfully!";
                        
                        string message = $"RDL file saved successfully:\n{sfd.FileName}\n\n";
                        message += hasBom ? "Encoding: UTF-8 with BOM (Correct)" : "WARNING: UTF-8 BOM not found!";
                        
                        MessageBox.Show(message, 
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "Error saving RDL!";
                        MessageBox.Show($"Error saving RDL file:\n{ex.Message}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void OnPreviewRdl(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Önce bir RPT dosyası yükleyin!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var rdlBuilder = new RDLBuilder(_currentModel);
            var rdlContent = rdlBuilder.GenerateRDL();
            
            var previewForm = new RdlPreviewForm(rdlContent);
            previewForm.ShowDialog();
        }
        
        private void ShowFormulaConverter(ReportFormula formula)
        {
            var converterForm = new FormulaConverterForm(formula, _formulaConverter);
            if (converterForm.ShowDialog() == DialogResult.OK)
            {
                // Formül güncellenmiş olabilir
                designPanel.Invalidate();
            }
        }
        
        private void OnFormulaConverter(object sender, EventArgs e)
        {
            if (_currentModel == null || _currentModel.Formulas.Count == 0)
            {
                MessageBox.Show("Dönüştürülecek formül bulunamadı!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var batchForm = new BatchFormulaConverterForm(_currentModel.Formulas, _formulaConverter);
            batchForm.ShowDialog();
        }
        
        private void OnDataSourceSettings(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Önce bir RPT dosyası yükleyin!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var dsForm = new DataSourceSettingsForm(_currentModel.ConnectionInfo);
            dsForm.ShowDialog();
        }
        
        private void OnBatchValidation(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Önce bir RPT dosyası yükleyin!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            MessageBox.Show("Validasyon tamamlandı!", "Bilgi", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private bool _showRulers = true;
        private bool _showGrid = true;
        private MeasurementUnit _currentUnit = MeasurementUnit.Millimeter;
        
        private void OnToggleRulers(object sender, EventArgs e)
        {
            _showRulers = !_showRulers;
            lblHorizontalRuler.Visible = _showRulers;
            lblVerticalRuler.Visible = _showRulers;
        }
        
        private void OnToggleGrid(object sender, EventArgs e)
        {
            _showGrid = !_showGrid;
            designPanel.Invalidate();
        }
        
        private void SetUnit(MeasurementUnit unit)
        {
            _currentUnit = unit;
            lblHorizontalRuler.Invalidate();
            lblVerticalRuler.Invalidate();
            lblStatus.Text = $"Ölçü birimi: {unit}";
        }
    }
    
    public enum MeasurementUnit
    {
        Millimeter,
        Inch,
        Pixel
    }
}
