using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalToSSRS.Models;
using System;
using System.Collections.Generic;

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
            
            try
            {
                // File exists check
                if (!System.IO.File.Exists(rptFilePath))
                {
                    throw new System.IO.FileNotFoundException($"RPT file not found: {rptFilePath}");
                }
                
                // Load with retry logic
                LoadWithRetry(rptFilePath);
                
                var model = new CrystalReportModel
                {
                    ReportName = System.IO.Path.GetFileNameWithoutExtension(rptFilePath)
                };
                
                // Extract connection information
                model.ConnectionInfo = ExtractOracleConnectionInfo();
                
                // Extract sections
                model.Sections = ExtractSections();
                
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
                Console.WriteLine($"Fatal error parsing report: {ex.Message}");
                throw;
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
                    Height = obj.Height
                };
                
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
                }
                
                return reportObj;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting report object: {ex.Message}");
                return null;
            }
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
