using CrystalToSSRS.Models;
using System;
using System.Text;
using System.Xml;

namespace CrystalToSSRS.RDLGenerator
{
    public class RDLBuilder
    {
        private CrystalReportModel _model;
        private XmlDocument _xmlDoc;
        private XmlNamespaceManager _nsmgr;
        private const string RDL_NAMESPACE = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";
        
        // A4 dimensions (8.5" x 11")
        private const float PAGE_WIDTH = 8.5f;
        private const float PAGE_HEIGHT = 11.0f;
        private const float MARGIN = 0.5f;
        private const float CONTENT_WIDTH = PAGE_WIDTH - (2 * MARGIN);
        private const float CONTENT_HEIGHT = PAGE_HEIGHT - (2 * MARGIN);
        
        public RDLBuilder(CrystalReportModel model)
        {
            _model = model;
            _xmlDoc = new XmlDocument();
            _nsmgr = new XmlNamespaceManager(_xmlDoc.NameTable);
            _nsmgr.AddNamespace("rd", RDL_NAMESPACE);
        }
        
        public string GenerateRDL()
        {
            // Root element
            var report = CreateElement("Report");
            report.SetAttribute("xmlns", RDL_NAMESPACE);
            _xmlDoc.AppendChild(report);
            
            // DataSources
            var dataSources = CreateDataSources();
            report.AppendChild(dataSources);
            
            // DataSets
            var dataSets = CreateDataSets();
            report.AppendChild(dataSets);
            
            // ReportParameters
            if (_model.Parameters != null && _model.Parameters.Count > 0)
            {
                var parameters = CreateReportParameters();
                report.AppendChild(parameters);
            }
            
            // Variables for formulas
            if (_model.Formulas != null && _model.Formulas.Count > 0)
            {
                var variables = CreateVariables();
                if (variables.ChildNodes.Count > 0)
                {
                    report.AppendChild(variables);
                }
            }
            
            // Body
            var body = CreateBody();
            report.AppendChild(body);
            
            // Width and height
            AppendElement(report, "Width", $"{PAGE_WIDTH}in");
            
            // Page settings
            var page = CreatePageSettings();
            report.AppendChild(page);
            
            return FormatXml(_xmlDoc);
        }
        
        private XmlElement CreateDataSources()
        {
            var dataSources = CreateElement("DataSources");
            var dataSource = CreateElement("DataSource");
            dataSource.SetAttribute("Name", "OracleDataSource");
            dataSource.SetAttribute("Type", "");
            
            var connProps = CreateOracleConnectionString();
            dataSource.AppendChild(connProps);
            
            dataSources.AppendChild(dataSource);
            return dataSources;
        }
        
        private XmlElement CreateOracleConnectionString()
        {
            var connProps = CreateElement("ConnectionProperties");
            
            var connInfo = _model.ConnectionInfo;
            
            string connectionString = string.Empty;
            if (connInfo != null && !string.IsNullOrEmpty(connInfo.ServerName))
            {
                connectionString = BuildOracleConnectionString(connInfo);
            }
            else
            {
                connectionString = "Data Source=;User Id=;";
            }
            
            AppendElement(connProps, "DataProvider", "ORACLE");
            AppendElement(connProps, "ConnectString", connectionString);
            AppendElement(connProps, "IntegratedSecurity", "false");
            
            return connProps;
        }
        
        private string BuildOracleConnectionString(OracleConnectionInfo connInfo)
        {
            var sb = new StringBuilder();
            sb.Append("Data Source=(DESCRIPTION=");
            sb.Append("(ADDRESS=(PROTOCOL=TCP)");
            sb.Append($"(HOST={connInfo.ServerName})");
            
            if (connInfo.Port > 0)
            {
                sb.Append($"(PORT={connInfo.Port})");
            }
            else
            {
                sb.Append("(PORT=1521)");
            }
            
            sb.Append(")");
            sb.Append("(CONNECT_DATA=");
            
            if (!string.IsNullOrEmpty(connInfo.ServiceName))
            {
                sb.Append($"(SERVICE_NAME={connInfo.ServiceName})");
            }
            else if (!string.IsNullOrEmpty(connInfo.DatabaseName))
            {
                sb.Append($"(SID={connInfo.DatabaseName})");
            }
            
            sb.Append(")");
            sb.Append(");");
            
            if (!string.IsNullOrEmpty(connInfo.UserId))
            {
                sb.Append($"User Id={connInfo.UserId};");
            }
            
            return sb.ToString();
        }
        
        private XmlElement CreateDataSets()
        {
            var dataSets = CreateElement("DataSets");
            
            // Create dataset for each table
            if (_model.Tables != null)
            {
                foreach (var table in _model.Tables)
                {
                    var dataSet = CreateDataSet(table);
                    dataSets.AppendChild(dataSet);
                }
            }
            
            // If no tables, create a default empty dataset
            if (_model.Tables == null || _model.Tables.Count == 0)
            {
                var dataSet = CreateDefaultDataSet();
                dataSets.AppendChild(dataSet);
            }
            
            return dataSets;
        }
        
        private XmlElement CreateDataSet(DatabaseTable table)
        {
            var dataSet = CreateElement("DataSet");
            dataSet.SetAttribute("Name", SanitizeName(table.Alias ?? table.Name));
            
            // Query
            var query = CreateElement("Query");
            AppendElement(query, "DataSourceName", "OracleDataSource");
            AppendElement(query, "CommandText", $"SELECT * FROM {table.Name}");
            dataSet.AppendChild(query);
            
            // Fields - each field as separate item
            var fields = CreateElement("Fields");
            if (table.Fields != null)
            {
                foreach (var field in table.Fields)
                {
                    var fieldElement = CreateElement("Field");
                    fieldElement.SetAttribute("Name", SanitizeName(field.Name));
                    
                    AppendElement(fieldElement, "DataField", field.Name);
                    AppendElement(fieldElement, "rd:TypeName", ConvertDataType(field.DataType));
                    
                    fields.AppendChild(fieldElement);
                }
            }
            dataSet.AppendChild(fields);
            
            return dataSet;
        }
        
        private XmlElement CreateDefaultDataSet()
        {
            var dataSet = CreateElement("DataSet");
            dataSet.SetAttribute("Name", "DefaultDataSet");
            
            var query = CreateElement("Query");
            AppendElement(query, "DataSourceName", "OracleDataSource");
            AppendElement(query, "CommandText", "SELECT 1 AS Id");
            dataSet.AppendChild(query);
            
            var fields = CreateElement("Fields");
            var fieldElement = CreateElement("Field");
            fieldElement.SetAttribute("Name", "Id");
            AppendElement(fieldElement, "DataField", "Id");
            AppendElement(fieldElement, "rd:TypeName", "System.Int32");
            fields.AppendChild(fieldElement);
            dataSet.AppendChild(fields);
            
            return dataSet;
        }
        
        private XmlElement CreateReportParameters()
        {
            var parameters = CreateElement("ReportParameters");
            
            if (_model.Parameters != null)
            {
                foreach (var param in _model.Parameters)
                {
                    var parameter = CreateElement("ReportParameter");
                    parameter.SetAttribute("Name", SanitizeName(param.Name));
                    
                    AppendElement(parameter, "DataType", ConvertDataType(param.DataType));
                    AppendElement(parameter, "Prompt", param.PromptText ?? param.Name);
                    AppendElement(parameter, "AllowBlank", "true");
                    
                    if (param.AllowMultipleValue)
                    {
                        AppendElement(parameter, "MultiValue", "true");
                    }
                    
                    if (param.DefaultValue != null)
                    {
                        var defaultValues = CreateElement("DefaultValue");
                        var values = CreateElement("Values");
                        AppendElement(values, "Value", param.DefaultValue.ToString());
                        defaultValues.AppendChild(values);
                        parameter.AppendChild(defaultValues);
                    }
                    
                    parameters.AppendChild(parameter);
                }
            }
            
            return parameters;
        }
        
        private XmlElement CreateVariables()
        {
            var variables = CreateElement("Variables");
            
            if (_model.Formulas != null)
            {
                foreach (var formula in _model.Formulas)
                {
                    var variable = CreateElement("Variable");
                    variable.SetAttribute("Name", SanitizeName(formula.Name));
                    
                    // Store formula text as is (will need conversion)
                    AppendElement(variable, "Value", formula.FormulaText ?? "");
                    
                    variables.AppendChild(variable);
                }
            }
            
            return variables;
        }
        
        private XmlElement CreateBody()
        {
            var body = CreateElement("Body");
            var reportItems = CreateElement("ReportItems");
            
            // Create table with all fields displayed individually
            var tablix = CreateDetailedTablix();
            reportItems.AppendChild(tablix);
            
            body.AppendChild(reportItems);
            AppendElement(body, "Height", $"{CONTENT_HEIGHT}in");
            
            return body;
        }
        
        private XmlElement CreateDetailedTablix()
        {
            var tablix = CreateElement("Tablix");
            tablix.SetAttribute("Name", "FieldDetailsTable");
            
            // Get first table's fields for display
            var fieldsToShow = new System.Collections.Generic.List<TableField>();
            if (_model.Tables != null && _model.Tables.Count > 0)
            {
                fieldsToShow = _model.Tables[0].Fields;
            }
            
            // TablixCorner
            var corner = CreateTablixCorner();
            
            // TablixBody with rows and columns
            var tablixBody = CreateElement("TablixBody");
            var tablixColumns = CreateElement("TablixColumns");
            var tablixRows = CreateElement("TablixRows");
            
            // Create columns for each field
            float colWidth = (CONTENT_WIDTH - 1.0f) / Math.Max(fieldsToShow.Count, 1);
            
            foreach (var field in fieldsToShow)
            {
                AppendElement(tablixColumns, "TablixColumn", null);
                var colWidth_elem = CreateElement("Width");
                colWidth_elem.InnerText = $"{colWidth}in";
                tablixColumns.LastChild?.AppendChild(colWidth_elem);
            }
            
            // Header row
            var headerRow = CreateTablixRow(fieldsToShow, isHeader: true);
            tablixRows.AppendChild(headerRow);
            
            // Detail row
            var detailRow = CreateTablixRow(fieldsToShow, isHeader: false);
            tablixRows.AppendChild(detailRow);
            
            tablixBody.AppendChild(tablixColumns);
            tablixBody.AppendChild(tablixRows);
            tablix.AppendChild(tablixBody);
            
            // DataSetName
            if (_model.Tables != null && _model.Tables.Count > 0)
            {
                AppendElement(tablix, "DataSetName", SanitizeName(_model.Tables[0].Alias ?? _model.Tables[0].Name));
            }
            
            AppendElement(tablix, "Top", $"{MARGIN}in");
            AppendElement(tablix, "Left", $"{MARGIN}in");
            AppendElement(tablix, "Height", $"{CONTENT_HEIGHT * 0.5}in");
            AppendElement(tablix, "Width", $"{CONTENT_WIDTH}in");
            
            return tablix;
        }
        
        private XmlElement CreateTablixCorner()
        {
            var corner = CreateElement("TablixCorner");
            // Corner cell for row headers
            return corner;
        }
        
        private XmlElement CreateTablixRow(System.Collections.Generic.List<TableField> fields, bool isHeader)
        {
            var row = CreateElement("TablixRow");
            
            foreach (var field in fields)
            {
                var cell = CreateElement("TablixCell");
                var reportItem = CreateElement("ReportItem");
                
                reportItem.SetAttribute("Name", SanitizeName(field.Name));
                
                // Create textbox
                var textbox = CreateElement("Textbox");
                textbox.SetAttribute("Name", SanitizeName(field.Name) + "TextBox");
                
                if (isHeader)
                {
                    AppendElement(textbox, "CanGrow", "true");
                    AppendElement(textbox, "CanShrink", "false");
                    AppendElement(textbox, "Value", field.Name);
                }
                else
                {
                    AppendElement(textbox, "CanGrow", "true");
                    AppendElement(textbox, "CanShrink", "false");
                    AppendElement(textbox, "Value", $"=Fields!{SanitizeName(field.Name)}.Value");
                }
                
                // Styling
                var style = CreateElement("Style");
                if (isHeader)
                {
                    AppendElement(style, "BackgroundColor", "LightGray");
                    AppendElement(style, "FontWeight", "Bold");
                }
                AppendElement(style, "BorderStyle", "Solid");
                AppendElement(style, "BorderWidth", "1pt");
                AppendElement(style, "BorderColor", "Black");
                AppendElement(style, "PaddingLeft", "2pt");
                AppendElement(style, "PaddingRight", "2pt");
                textbox.AppendChild(style);
                
                reportItem.AppendChild(textbox);
                cell.AppendChild(reportItem);
                row.AppendChild(cell);
            }
            
            return row;
        }
        
        private XmlElement CreatePageSettings()
        {
            var page = CreateElement("Page");
            
            AppendElement(page, "PageHeight", $"{PAGE_HEIGHT}in");
            AppendElement(page, "PageWidth", $"{PAGE_WIDTH}in");
            
            var leftMargin = CreateElement("LeftMargin");
            leftMargin.InnerText = $"{MARGIN}in";
            page.AppendChild(leftMargin);
            
            var rightMargin = CreateElement("RightMargin");
            rightMargin.InnerText = $"{MARGIN}in";
            page.AppendChild(rightMargin);
            
            var topMargin = CreateElement("TopMargin");
            topMargin.InnerText = $"{MARGIN}in";
            page.AppendChild(topMargin);
            
            var bottomMargin = CreateElement("BottomMargin");
            bottomMargin.InnerText = $"{MARGIN}in";
            page.AppendChild(bottomMargin);
            
            return page;
        }
        
        private XmlElement CreateElement(string name)
        {
            return _xmlDoc.CreateElement(name, RDL_NAMESPACE);
        }
        
        private void AppendElement(XmlElement parent, string childName, string value)
        {
            var child = CreateElement(childName);
            if (value != null)
            {
                child.InnerText = value;
            }
            parent.AppendChild(child);
        }
        
        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Field";
            
            // Remove invalid characters for SSRS names
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            
            var result = sb.ToString();
            if (result.Length == 0)
                result = "Field";
            
            return result;
        }
        
        private string ConvertDataType(string crystalDataType)
        {
            if (string.IsNullOrEmpty(crystalDataType))
                return "System.String";
            
            // Convert Crystal Reports data types to SSRS data types
            var upper = crystalDataType.ToUpper();
            
            if (upper.Contains("VARCHAR") || upper.Contains("STRING") || upper.Contains("CHAR"))
                return "System.String";
            else if (upper.Contains("NUMBER") || upper.Contains("DECIMAL"))
                return "System.Decimal";
            else if (upper.Contains("INT"))
                return "System.Int32";
            else if (upper.Contains("DATE") || upper.Contains("DATETIME") || upper.Contains("TIMESTAMP"))
                return "System.DateTime";
            else if (upper.Contains("BOOLEAN") || upper.Contains("BIT"))
                return "System.Boolean";
            else if (upper.Contains("FLOAT") || upper.Contains("DOUBLE"))
                return "System.Double";
            
            return "System.String";
        }
        
        private string FormatXml(XmlDocument doc)
        {
            var sb = new StringBuilder();
            
            // Add XML declaration with UTF-8 BOM
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = System.Text.Encoding.UTF8,
                OmitXmlDeclaration = true  // We're adding it manually
            };
            
            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            
            return sb.ToString();
        }
        
        public void SaveToFile(string filePath)
        {
            try
            {
                var rdlContent = GenerateRDL();
                
                // UTF-8 with BOM encoding (UTF-8 BOM: EF BB BF)
                var encoding = new System.Text.UTF8Encoding(true);
                System.IO.File.WriteAllText(filePath, rdlContent, encoding);
                
                Console.WriteLine($"RDL file successfully saved with UTF-8 BOM: {filePath}");
                
                // Verify BOM was written
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
                {
                    Console.WriteLine("UTF-8 BOM verification: OK");
                }
                else
                {
                    Console.WriteLine("WARNING: UTF-8 BOM not found in file!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving RDL file: {ex.Message}");
                throw;
            }
        }
    }
}
