using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalToSSRS.Models;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CrystalToSSRS.Converters
{
    public class CrystalReportParser
    {
        private ReportDocument _report;
        private const int MAX_RETRY = 3;
        private const int RETRY_DELAY = 100; // milliseconds
        
        // Helper method to save RDL with UTF-8 BOM
        public static void SaveRdlWithProperEncoding(string filePath, string rdlContent)
        {
            try
            {
                // UTF-8 with BOM encoding
                var encoding = new System.Text.UTF8Encoding(true);
                System.IO.File.WriteAllText(filePath, rdlContent, encoding);
                Console.WriteLine($"RDL file saved with UTF-8 BOM: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving RDL file: {ex.Message}");
                throw;
            }
        }
        
        public CrystalReportModel ParseReport(string rptFilePath)
        {
            _report = new ReportDocument();
            
            CrystalReportModel model = null;
            try
            {
                // File exists check
                if (!System.IO.File.Exists(rptFilePath))
                {
                    throw new System.IO.FileNotFoundException($"RPT file not found: {rptFilePath}");
                }
                
                // Load with retry logic
                LoadWithRetry(rptFilePath);
                
                model = new CrystalReportModel
                {
                    ReportName = System.IO.Path.GetFileNameWithoutExtension(rptFilePath)
                };
                
                // Extract connection information
                model.ConnectionInfo = ExtractOracleConnectionInfo();
                
                // Extract sections
                model.Sections = ExtractSectionsSafe(model);
                
                // Extract parameters
                model.Parameters = ExtractParameters();
                
                // Extract formulas
                model.Formulas = ExtractFormulas();
                
                // Extract tables and fields
                model.Tables = ExtractTables();
                model.Fields = ExtractFields();
                
                return model;
            }
            catch (Exception ex)
            {
                if (model == null) model = new CrystalReportModel();
                model.ParseErrors.Add($"Fatal: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Fatal error parsing report: {ex.Message}");
                return model;
            }
            finally
            {
                // Dispose report if loaded
                if (_report != null)
                {
                    try
                    {
                        _report.Close();
                        _report.Dispose();
                    }
                    catch { }
                }
            }
        }

        private List<ReportSection> ExtractSectionsSafe(CrystalReportModel model)
        {
            var sections = new List<ReportSection>();
            try
            {
                foreach (Section section in _report.ReportDefinition.Sections)
                {
                    var reportSection = new ReportSection
                    {
                        Name = section.Name ?? "Unknown",
                        Kind = section.Kind.ToString(),
                        Height = section.Height
                    };

                    try
                    {
                        foreach (CrystalDecisions.CrystalReports.Engine.ReportObject obj in section.ReportObjects)
                        {
                            try
                            {
                                var reportObj = ExtractReportObject(obj);
                                if (reportObj != null)
                                {
                                    reportSection.Objects.Add(reportObj);
                                }
                            }
                            catch (Exception exObj)
                            {
                                model.ParseErrors.Add($"Object '{obj?.Name}' in section '{section?.Name}': {exObj.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        model.ParseErrors.Add($"Section '{section?.Name}': {ex.Message}");
                    }

                    sections.Add(reportSection);
                }
            }
            catch (Exception ex)
            {
                model.ParseErrors.Add($"Sections root: {ex.Message}");
            }

            return sections;
        }
        
        private void LoadWithRetry(string rptFilePath)
        {
            int attempt = 0;
            Exception lastException = null;
            
            while (attempt < MAX_RETRY)
            {
                try
                {
                    _report.Load(rptFilePath);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;
                    
                    if (attempt < MAX_RETRY)
                    {
                        Console.WriteLine($"Load attempt {attempt} failed, retrying...");
                        System.Threading.Thread.Sleep(RETRY_DELAY);
                    }
                }
            }
            
            throw new Exception($"Failed to load RPT file after {MAX_RETRY} attempts", lastException);
        }
        
        private OracleConnectionInfo ExtractOracleConnectionInfo()
        {
            var connInfo = new OracleConnectionInfo();
            
            try
            {
                if (_report.Database.Tables.Count > 0)
                {
                    var table = _report.Database.Tables[0];
                    
                    if (table.LogOnInfo != null)
                    {
                        var logonInfo = table.LogOnInfo;
                        
                        connInfo.ServerName = logonInfo.ConnectionInfo?.ServerName ?? "";
                        connInfo.DatabaseName = logonInfo.ConnectionInfo?.DatabaseName ?? "";
                        connInfo.UserId = logonInfo.ConnectionInfo?.UserID ?? "";
                        
                        // Parse service name from connection string
                        if (!string.IsNullOrEmpty(logonInfo.ConnectionInfo?.ServerName))
                        {
                            // Format: hostname:port/servicename
                            var parts = logonInfo.ConnectionInfo.ServerName.Split(':');
                            if (parts.Length > 0)
                            {
                                connInfo.ServerName = parts[0];
                                
                                if (parts.Length > 1)
                                {
                                    var portService = parts[1].Split('/');
                                    if (portService.Length > 0)
                                    {
                                        int.TryParse(portService[0], out int port);
                                        connInfo.Port = port > 0 ? port : 1521;
                                    }
                                    if (portService.Length > 1)
                                    {
                                        connInfo.ServiceName = portService[1];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting connection info: {ex.Message}");
            }
            
            return connInfo;
        }
        
        private List<ReportSection> ExtractSections()
        {
            var sections = new List<ReportSection>();
            
            try
            {
                foreach (Section section in _report.ReportDefinition.Sections)
                {
                    var reportSection = new ReportSection
                    {
                        Name = section.Name ?? "Unknown",
                        Kind = section.Kind.ToString(),
                        Height = section.Height
                    };
                    
                    // Extract objects from section
                    try
                    {
                        foreach (CrystalDecisions.CrystalReports.Engine.ReportObject obj in section.ReportObjects)
                        {
                            var reportObj = ExtractReportObject(obj);
                            if (reportObj != null)
                            {
                                reportSection.Objects.Add(reportObj);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting objects from section {section.Name}: {ex.Message}");
                    }
                    
                    sections.Add(reportSection);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting sections: {ex.Message}");
            }
            
            return sections;
        }
        
        private Models.ReportObject ExtractReportObject(CrystalDecisions.CrystalReports.Engine.ReportObject obj)
        {
            try
            {
                var reportObj = new Models.ReportObject
                {
                    Name = obj.Name ?? "Unnamed",
                    Type = obj.Kind.ToString(),
                    Left = obj.Left,
                    Top = obj.Top,
                    Width = obj.Width,
                    Height = obj.Height,
                    Style = new StyleInfo()
                };
                
                // Common ObjectFormat (Suppress, alignment etc.) via reflection for compatibility
                try
                {
                    var fmt = GetProp(obj, "ObjectFormat");
                    if (fmt != null)
                    {
                        var suppress = GetProp(fmt, "EnableSuppress") as bool?;
                        if (suppress.HasValue) reportObj.Suppress = suppress.Value;
                        reportObj.Style.TextAlign = (GetProp(fmt, "HorizontalAlignment") ?? GetProp(fmt, "HorizontalAlignment2"))?.ToString();
                        reportObj.Style.VerticalAlign = (GetProp(fmt, "VerticalAlignment") ?? GetProp(fmt, "VerticalAlignment2"))?.ToString();
                    }
                }
                catch { }

                // Text object
                if (obj is TextObject textObj)
                {
                    reportObj.Text = textObj.Text ?? "";
                    
                    if (textObj.Font != null)
                    {
                        reportObj.Font = new FontInfo
                        {
                            Name = textObj.Font.Name ?? "Arial",
                            Size = textObj.Font.Size,
                            Bold = textObj.Font.Bold,
                            Italic = textObj.Font.Italic,
                            Underline = textObj.Font.Underline
                        };
                    }

                    // Colors and background via reflection
                    TrySetColors(reportObj.Style, textObj);
                    // Rotation if available
                    var rot = GetProp(textObj, "RotationAngle") as int?;
                    if (rot.HasValue) reportObj.Style.Rotation = rot.Value;
                }
                // Field object
                else if (obj is FieldObject fieldObj)
                {
                    reportObj.DataSource = fieldObj.DataSource != null ? fieldObj.DataSource.ToString() : obj.Name;
                    
                    if (fieldObj.Font != null)
                    {
                        reportObj.Font = new FontInfo
                        {
                            Name = fieldObj.Font.Name ?? "Arial",
                            Size = fieldObj.Font.Size,
                            Bold = fieldObj.Font.Bold,
                            Italic = fieldObj.Font.Italic,
                            Underline = fieldObj.Font.Underline
                        };
                    }

                    TrySetColors(reportObj.Style, fieldObj);
                }
                // Line object
                else if (obj is LineObject lineObj)
                {
                    reportObj.Text = "<Line>";
                    // Line thickness
                    var lw = GetProp(lineObj, "LineWidth") as int?;
                    if (lw.HasValue) reportObj.Style.LineWidthPt = lw.Value;
                }
                // Box object (frame)
                else if (obj is BoxObject boxObj)
                {
                    reportObj.Text = "<Box>";
                    // Border width/style
                    var bw = GetProp(boxObj, "BorderWidth") as int?;
                    if (bw.HasValue) reportObj.Style.BorderWidthPt = bw.Value;
                    reportObj.Style.BorderStyle = (GetProp(boxObj, "BorderStyle") ?? GetProp(boxObj, "LineStyle"))?.ToString();
                }
                // Picture object (image)
                else if (obj is PictureObject picObj)
                {
                    reportObj.Text = "<Image>";
                    try
                    {
                        // Use reflection to get optional GraphicLocation property if available
                        var prop = picObj.GetType().GetProperty("GraphicLocation");
                        if (prop != null)
                        {
                            var val = prop.GetValue(picObj, null);
                            if (val != null)
                                reportObj.DataSource = val.ToString();
                        }
                    }
                    catch { }
                }
                // Subreport object
                else if (obj is SubreportObject subObj)
                {
                    reportObj.Text = "<Subreport>";
                    try { reportObj.DataSource = subObj.SubreportName; } catch { }
                }
                
                return reportObj;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting report object: {ex.Message}");
                return null;
            }
        }

        private void TrySetColors(StyleInfo style, object crystalObj)
        {
            try
            {
                // Foreground Color
                var fg = GetProp(crystalObj, "Color");
                int argb;
                if (TryGetColorArgb(fg, out argb)) style.ForeColorArgb = argb;
                // Background Color
                var bg = GetProp(crystalObj, "BackgroundColor");
                if (TryGetColorArgb(bg, out argb)) style.BackColorArgb = argb;
                // Border (if has Border object)
                var border = GetProp(crystalObj, "Border");
                if (border != null)
                {
                    var bcol = GetProp(border, "Color");
                    if (TryGetColorArgb(bcol, out argb)) style.BorderColorArgb = argb;
                    var bw = GetProp(border, "Width") as int?;
                    if (bw.HasValue) style.BorderWidthPt = bw.Value;
                    var bs = GetProp(border, "Style");
                    if (bs != null) style.BorderStyle = bs.ToString();
                }
            }
            catch { }
        }

        private object GetProp(object obj, string name)
        {
            if (obj == null) return null;
            try
            {
                var pi = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                return pi?.GetValue(obj, null);
            }
            catch { return null; }
        }

        private bool TryGetColorArgb(object colorObj, out int argb)
        {
            argb = 0;
            if (colorObj == null) return false;
            try
            {
                // If already System.Drawing.Color
                var t = colorObj.GetType();
                if (t.FullName == "System.Drawing.Color")
                {
                    var a = (int)t.GetProperty("A").GetValue(colorObj, null);
                    var r = (int)t.GetProperty("R").GetValue(colorObj, null);
                    var g = (int)t.GetProperty("G").GetValue(colorObj, null);
                    var b = (int)t.GetProperty("B").GetValue(colorObj, null);
                    argb = (a << 24) | (r << 16) | (g << 8) | b;
                    return true;
                }
                // Crystal color may expose ToArgb
                var toArgb = t.GetMethod("ToArgb", Type.EmptyTypes);
                if (toArgb != null)
                {
                    argb = (int)toArgb.Invoke(colorObj, null);
                    return true;
                }
                // Try parse from string #RRGGBB
                var s = colorObj.ToString();
                if (!string.IsNullOrEmpty(s) && s.StartsWith("#") && s.Length == 7)
                {
                    argb = unchecked((int)(0xFF000000 | Convert.ToInt32(s.Substring(1), 16)));
                    return true;
                }
            }
            catch { }
            return false;
        }
        
        private List<ReportParameter> ExtractParameters()
        {
            var parameters = new List<ReportParameter>();
            
            try
            {
                if (_report.DataDefinition == null || _report.DataDefinition.ParameterFields == null)
                    return parameters;
                
                foreach (ParameterFieldDefinition param in _report.DataDefinition.ParameterFields)
                {
                    // Skip system parameters
                    if (param.Name != null && param.Name.StartsWith("?"))
                        continue;
                    
                    try
                    {
                        var dataType = param.ValueType.ToString();
                        
                        parameters.Add(new ReportParameter
                        {
                            Name = param.Name ?? "Unknown",
                            DataType = dataType ?? "Unknown",
                            PromptText = param.PromptText ?? "",
                            AllowMultipleValue = false,
                            DefaultValue = ExtractParameterDefaultValue(param)
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting parameter {param?.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting parameters: {ex.Message}");
            }
            
            return parameters;
        }
        
        private object ExtractParameterDefaultValue(ParameterFieldDefinition param)
        {
            try
            {
                if (param.HasCurrentValue && param.CurrentValues != null && param.CurrentValues.Count > 0)
                {
                    return param.CurrentValues[0].ToString();
                }
            }
            catch { }
            
            return null;
        }
        
        private List<ReportFormula> ExtractFormulas()
        {
            var formulas = new List<ReportFormula>();
            
            try
            {
                if (_report.DataDefinition == null || _report.DataDefinition.FormulaFields == null)
                    return formulas;
                
                foreach (FormulaFieldDefinition formula in _report.DataDefinition.FormulaFields)
                {
                    try
                    {
                        var dataType = formula.ValueType.ToString();
                        
                        formulas.Add(new ReportFormula
                        {
                            Name = formula.Name ?? "Unknown",
                            FormulaText = formula.Text ?? "",
                            DataType = dataType
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting formula {formula?.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting formulas: {ex.Message}");
            }
            
            return formulas;
        }
        
        private List<DatabaseTable> ExtractTables()
        {
            var tables = new List<DatabaseTable>();
            
            try
            {
                if (_report.Database == null || _report.Database.Tables == null || _report.Database.Tables.Count == 0)
                    return tables;
                
                foreach (Table table in _report.Database.Tables)
                {
                    try
                    {
                        var dbTable = new DatabaseTable
                        {
                            Name = table.Name ?? "Unknown",
                            Alias = table.Name ?? "Unknown"
                        };
                        
                        if (table.Fields != null)
                        {
                            foreach (DatabaseFieldDefinition field in table.Fields)
                            {
                                try
                                {
                                    var dataType = field.ValueType.ToString();
                                    
                                    dbTable.Fields.Add(new TableField
                                    {
                                        Name = field.Name ?? "Unknown",
                                        DataType = dataType,
                                        Length = 0
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error extracting field {field?.Name}: {ex.Message}");
                                }
                            }
                        }
                        
                        tables.Add(dbTable);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting table {table?.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting tables: {ex.Message}");
            }
            
            return tables;
        }
        
        private List<ReportField> ExtractFields()
        {
            var fields = new List<ReportField>();
            
            try
            {
                if (_report.Database == null || _report.Database.Tables == null)
                    return fields;
                
                foreach (Table table in _report.Database.Tables)
                {
                    try
                    {
                        if (table.Fields != null)
                        {
                            foreach (DatabaseFieldDefinition field in table.Fields)
                            {
                                try
                                {
                                    var dataType = field.ValueType.ToString();
                                    
                                    fields.Add(new ReportField
                                    {
                                        Name = field.Name ?? "Unknown",
                                        TableName = table.Name ?? "Unknown",
                                        DataType = dataType
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error adding field: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing table {table?.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting fields: {ex.Message}");
            }
            
            return fields;
        }
    }
}
