using CrystalToSSRS.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;

namespace CrystalToSSRS.RDLGenerator
{
    public class RDLBuilder
    {
        private CrystalReportModel _model;
        private XmlDocument _xmlDoc;
        private XmlNamespaceManager _nsmgr;
        private const string RDL_NAMESPACE = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";
        private const string RD_NAMESPACE = "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner";
        private const string AM_NAMESPACE = "http://schemas.microsoft.com/sqlserver/reporting/authoringmetadata";

        // A4 dimensions in mm
        private const string PAGE_WIDTH_MM = "210mm";
        private const string PAGE_HEIGHT_MM = "297mm";
        private const string MARGIN_MM = "20mm";

        public RDLBuilder(CrystalReportModel model)
        {
            _model = model;
            _xmlDoc = new XmlDocument();
            _nsmgr = new XmlNamespaceManager(_xmlDoc.NameTable);
            _nsmgr.AddNamespace("rdl", RDL_NAMESPACE);
            _nsmgr.AddNamespace("rd", RD_NAMESPACE);
            _nsmgr.AddNamespace("am", AM_NAMESPACE);
        }

        public string GenerateRDL()
        {
            // Root element with namespaces
            var report = _xmlDoc.CreateElement("Report", RDL_NAMESPACE);
            // add xmlns:rd and xmlns:am
            report.SetAttribute("xmlns:rd", RD_NAMESPACE);
            report.SetAttribute("xmlns:am", AM_NAMESPACE);
            _xmlDoc.AppendChild(report);

            // rd:ReportUnitType Mm
            var rdUnit = _xmlDoc.CreateElement("rd", "ReportUnitType", RD_NAMESPACE);
            rdUnit.InnerText = "Mm";
            report.AppendChild(rdUnit);

            // AutoRefresh
            AppendElement(report, "AutoRefresh", "0");

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

            // ReportSections -> ReportSection
            var reportSections = CreateElement("ReportSections");
            var reportSection = CreateElement("ReportSection");

            // Body
            var body = CreateBody();
            reportSection.AppendChild(body);

            // Width at ReportSection level
            AppendElement(reportSection, "Width", PAGE_WIDTH_MM);

            // Page settings
            var page = CreatePageSettings();
            reportSection.AppendChild(page);

            reportSections.AppendChild(reportSection);
            report.AppendChild(reportSections);

            return FormatXml(_xmlDoc);
        }

        private XmlElement CreateDataSources()
        {
            var dataSources = CreateElement("DataSources");
            var dataSource = CreateElement("DataSource");
            dataSource.SetAttribute("Name", "DataSource1");

            // Prefer DataSourceReference if not enough connection info
            bool hasConn = _model.ConnectionInfo != null && !string.IsNullOrWhiteSpace(_model.ConnectionInfo.ServerName);
            if (!hasConn)
            {
                var rdSecurity = _xmlDoc.CreateElement("rd", "SecurityType", RD_NAMESPACE);
                rdSecurity.InnerText = "None";
                dataSource.AppendChild(rdSecurity);

                AppendElement(dataSource, "DataSourceReference", "/DataSource1");

                var rdId = _xmlDoc.CreateElement("rd", "DataSourceID", RD_NAMESPACE);
                rdId.InnerText = Guid.NewGuid().ToString();
                dataSource.AppendChild(rdId);
            }
            else
            {
                var connProps = CreateOracleConnectionString();
                dataSource.AppendChild(connProps);
            }

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
            sb.Append("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)");
            sb.Append($"(HOST={connInfo.ServerName})");
            if (connInfo.Port > 0) sb.Append($"(PORT={connInfo.Port})"); else sb.Append("(PORT=1521)");
            sb.Append(")");
            sb.Append("(CONNECT_DATA=");
            if (!string.IsNullOrEmpty(connInfo.ServiceName)) sb.Append($"(SERVICE_NAME={connInfo.ServiceName})");
            else if (!string.IsNullOrEmpty(connInfo.DatabaseName)) sb.Append($"(SID={connInfo.DatabaseName})");
            sb.Append(")");
            sb.Append(")");
            sb.Append(");");
            if (!string.IsNullOrEmpty(connInfo.UserId)) sb.Append($"User Id={connInfo.UserId};");
            return sb.ToString();
        }

        private XmlElement CreateDataSets()
        {
            var dataSets = CreateElement("DataSets");

            if (_model.Tables != null && _model.Tables.Count > 0)
            {
                foreach (var table in _model.Tables)
                {
                    var dataSet = CreateDataSet(table);
                    dataSets.AppendChild(dataSet);
                }
            }
            else
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

            var query = CreateElement("Query");
            AppendElement(query, "DataSourceName", "DataSource1");
            AppendElement(query, "CommandText", $"SELECT * FROM {table.Name}");

            // Add QueryParameters from model parameters
            if (_model.Parameters != null && _model.Parameters.Count > 0)
            {
                var qps = CreateElement("QueryParameters");
                foreach (var p in _model.Parameters)
                {
                    var qp = CreateElement("QueryParameter");
                    // Name must be a valid RDL identifier; do not include ':'
                    qp.SetAttribute("Name", SanitizeName(p.Name));
                    AppendElement(qp, "Value", $"=Parameters!{SanitizeName(p.Name)}.Value");
                    qps.AppendChild(qp);
                }
                query.AppendChild(qps);
            }

            // rd:UseGenericDesigner
            var rdGeneric = _xmlDoc.CreateElement("rd", "UseGenericDesigner", RD_NAMESPACE);
            rdGeneric.InnerText = "true";
            query.AppendChild(rdGeneric);

            dataSet.AppendChild(query);

            var fields = CreateElement("Fields");
            if (table.Fields != null)
            {
                foreach (var field in table.Fields)
                {
                    var fieldElement = CreateElement("Field");
                    fieldElement.SetAttribute("Name", SanitizeName(field.Name));

                    AppendElement(fieldElement, "DataField", field.Name);

                    // rd:TypeName in correct namespace
                    var rdType = _xmlDoc.CreateElement("rd", "TypeName", RD_NAMESPACE);
                    rdType.InnerText = ConvertFieldClrType(field.DataType);
                    fieldElement.AppendChild(rdType);

                    fields.AppendChild(fieldElement);
                }
            }
            dataSet.AppendChild(fields);

            return dataSet;
        }

        private string GetQueryParameterName(string name)
        {
            // RDL Name cannot contain ':'; keep tokens (e.g., :Param) only in the SQL text if used.
            return SanitizeName(name);
        }

        private XmlElement CreateDefaultDataSet()
        {
            var dataSet = CreateElement("DataSet");
            dataSet.SetAttribute("Name", "DefaultDataSet");

            var query = CreateElement("Query");
            AppendElement(query, "DataSourceName", "DataSource1");
            AppendElement(query, "CommandText", "SELECT 1 AS Id");
            var rdGeneric = _xmlDoc.CreateElement("rd", "UseGenericDesigner", RD_NAMESPACE);
            rdGeneric.InnerText = "true";
            query.AppendChild(rdGeneric);
            dataSet.AppendChild(query);

            var fields = CreateElement("Fields");
            var fieldElement = CreateElement("Field");
            fieldElement.SetAttribute("Name", "Id");
            AppendElement(fieldElement, "DataField", "Id");
            var rdType = _xmlDoc.CreateElement("rd", "TypeName", RD_NAMESPACE);
            rdType.InnerText = "System.Int32";
            fieldElement.AppendChild(rdType);
            fields.AppendChild(fieldElement);
            dataSet.AppendChild(fields);

            return dataSet;
        }

        private XmlElement CreateReportParameters()
        {
            var parameters = CreateElement("ReportParameters");

            foreach (var param in _model.Parameters)
            {
                var parameter = CreateElement("ReportParameter");
                parameter.SetAttribute("Name", SanitizeName(param.Name));

                AppendElement(parameter, "DataType", ConvertParameterDataType(param.DataType));
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

            return parameters;
        }

        private XmlElement CreateBody()
        {
            var body = CreateElement("Body");
            var reportItems = CreateElement("ReportItems");

            var tablix = CreateDetailedTablix();
            reportItems.AppendChild(tablix);

            // Add Subreport items for Crystal subreport objects
            AddSubreports(reportItems);

            body.AppendChild(reportItems);
            AppendElement(body, "Height", "57.15mm");
            body.AppendChild(CreateElement("Style"));
            return body;
        }

        private void AddSubreports(XmlElement reportItems)
        {
            if (_model.Sections == null) return;
            foreach (var section in _model.Sections)
            {
                if (section?.Objects == null) continue;
                foreach (var obj in section.Objects)
                {
                    // Crystal parser sets Type from obj.Kind.ToString(), and for SubreportObject we also set Text = "<Subreport>"
                    var isSubreport = string.Equals(obj.Type, "SubreportObject", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(obj.Text, "<Subreport>", StringComparison.OrdinalIgnoreCase);
                    if (!isSubreport) continue;

                    var sub = CreateElement("Subreport");
                    sub.SetAttribute("Name", SanitizeName(obj.Name ?? obj.DataSource ?? "Subreport"));

                    // RDL: ReportName is the referenced report. Use DataSource (Crystal subreport name) when present
                    AppendElement(sub, "ReportName", SanitizeName(obj.DataSource ?? obj.Name ?? "Subreport"));

                    // Position and size from Crystal (twips to mm)
                    AppendElement(sub, "Top", TwipsToMm(obj.Top));
                    AppendElement(sub, "Left", TwipsToMm(obj.Left));
                    AppendElement(sub, "Height", TwipsToMm(obj.Height));
                    AppendElement(sub, "Width", TwipsToMm(obj.Width));

                    // Style/Border minimal
                    var style = CreateElement("Style");
                    var border = CreateElement("Border");
                    AppendElement(border, "Style", "None");
                    style.AppendChild(border);
                    sub.AppendChild(style);

                    // Parameters could be mapped here if available in model
                    // var parameters = CreateElement("Parameters");
                    // ... add Parameter elements
                    // sub.AppendChild(parameters);

                    reportItems.AppendChild(sub);
                }
            }
        }

        private string TwipsToMm(double twips)
        {
            // 1440 twips = 1 inch, 1 inch = 25.4 mm
            var mm = twips * 25.4 / 1440.0;
            return mm.ToString("0.###", CultureInfo.InvariantCulture) + "mm";
        }

        private XmlElement CreateDetailedTablix()
        {
            var tablix = CreateElement("Tablix");
            tablix.SetAttribute("Name", "Tablix1");

            // Determine fields to display
            var fieldsToShow = new List<TableField>();
            if (_model.Tables != null && _model.Tables.Count > 0)
                fieldsToShow = _model.Tables[0].Fields;

            // Example-based column widths (mm) - will apply in order
            var sampleWidths = new List<string>
            {
                "54.10417mm","32.40833mm","27.64583mm","60.98333mm","36.64167mm","36.37708mm",
                "36.90625mm","35.05417mm","31.08542mm","33.99583mm","35.58333mm","34.26042mm",
                "39.55208mm","35.05417mm","29.49792mm"
            };

            int colCount = Math.Max(1, fieldsToShow.Count);

            // TablixBody
            var body = CreateElement("TablixBody");
            var columns = CreateElement("TablixColumns");
            var rows = CreateElement("TablixRows");

            // Columns with sample widths when available
            for (int i = 0; i < colCount; i++)
            {
                var col = CreateElement("TablixColumn");
                string width = i < sampleWidths.Count ? sampleWidths[i] : "35mm";
                AppendElement(col, "Width", width);
                columns.AppendChild(col);
            }

            // Optional parameters row (spans all columns)
            if (_model.Parameters != null && _model.Parameters.Count > 0)
            {
                var prmRow = CreateElement("TablixRow");
                AppendElement(prmRow, "Height", "7.5875mm");
                var prmCells = CreateElement("TablixCells");
                for (int i = 0; i < colCount; i++)
                {
                    var cell = CreateElement("TablixCell");
                    if (i == 0)
                    {
                        var cellContents = CreateElement("CellContents");
                        var tb = CreateElement("Textbox");
                        tb.SetAttribute("Name", "ParametersLine");
                        AppendElement(tb, "CanGrow", "true");
                        AppendElement(tb, "KeepTogether", "true");
                        var paras = CreateElement("Paragraphs");
                        var para = CreateElement("Paragraph");
                        var textRuns = CreateElement("TextRuns");

                        // Build: Parametreler: Name: =Parameters!Name.Value, ...
                        var trIntro = CreateElement("TextRun");
                        AppendElement(trIntro, "Value", "Parametreler: ");
                        var trIntroStyle = CreateElement("Style");
                        AppendElement(trIntroStyle, "FontFamily", "Calibri");
                        AppendElement(trIntroStyle, "FontSize", "12pt");
                        AppendElement(trIntroStyle, "FontWeight", "Bold");
                        AppendElement(trIntroStyle, "Color", "White");
                        trIntro.AppendChild(trIntroStyle);
                        textRuns.AppendChild(trIntro);

                        for (int p = 0; p < _model.Parameters.Count; p++)
                        {
                            var prm = _model.Parameters[p];
                            // Label
                            var trLbl = CreateElement("TextRun");
                            AppendElement(trLbl, "Value", (p > 0 ? ", " : "") + prm.Name + ": ");
                            var trLblStyle = CreateElement("Style");
                            AppendElement(trLblStyle, "FontFamily", "Calibri");
                            AppendElement(trLblStyle, "FontSize", "12pt");
                            AppendElement(trLblStyle, "FontWeight", "Bold");
                            AppendElement(trLblStyle, "Color", "White");
                            trLbl.AppendChild(trLblStyle);
                            textRuns.AppendChild(trLbl);
                            // Value
                            var trVal = CreateElement("TextRun");
                            AppendElement(trVal, "Value", $"=Parameters!{SanitizeName(prm.Name)}.Value");
                            var trValStyle = CreateElement("Style");
                            AppendElement(trValStyle, "FontFamily", "Calibri");
                            AppendElement(trValStyle, "FontSize", "12pt");
                            AppendElement(trValStyle, "FontWeight", "Bold");
                            AppendElement(trValStyle, "Color", "White");
                            trVal.AppendChild(trValStyle);
                            textRuns.AppendChild(trVal);
                        }

                        para.AppendChild(textRuns);
                        para.AppendChild(CreateElement("Style"));
                        paras.AppendChild(para);
                        tb.AppendChild(paras);

                        var tbStyle = CreateElement("Style");
                        var border = CreateElement("Border");
                        AppendElement(border, "Color", "LightGrey");
                        AppendElement(border, "Style", "Solid");
                        tbStyle.AppendChild(border);
                        AppendElement(tbStyle, "BackgroundColor", "#305496");
                        AppendElement(tbStyle, "PaddingLeft", "2pt");
                        AppendElement(tbStyle, "PaddingRight", "2pt");
                        AppendElement(tbStyle, "PaddingTop", "2pt");
                        AppendElement(tbStyle, "PaddingBottom", "2pt");
                        tb.AppendChild(tbStyle);

                        cellContents.AppendChild(tb);
                        // ColSpan across all columns
                        AppendElement(cellContents, "ColSpan", colCount.ToString());
                        cell.AppendChild(cellContents);
                    }
                    // other cells remain empty to respect colspan
                    prmCells.AppendChild(cell);
                }
                prmRow.AppendChild(prmCells);
                rows.AppendChild(prmRow);
            }

            // Header row (style as sample)
            var headerRow = CreateElement("TablixRow");
            AppendElement(headerRow, "Height", "12.61458mm"); // sample header height
            var headerCells = CreateElement("TablixCells");
            for (int i = 0; i < colCount; i++)
            {
                var cell = CreateElement("TablixCell");
                var cellContents = CreateElement("CellContents");
                var tb = CreateElement("Textbox");
                tb.SetAttribute("Name", SanitizeName((fieldsToShow.Count > i ? fieldsToShow[i].Name : $"Col{i+1}") + "_Header"));
                AppendElement(tb, "CanGrow", "true");
                AppendElement(tb, "KeepTogether", "true");
                var paras = CreateElement("Paragraphs");
                var para = CreateElement("Paragraph");
                var textRuns = CreateElement("TextRuns");
                var tr = CreateElement("TextRun");
                AppendElement(tr, "Value", fieldsToShow.Count > i ? fieldsToShow[i].Name : $"Column {i+1}");
                var styleTr = CreateElement("Style");
                AppendElement(styleTr, "FontFamily", "Calibri");
                AppendElement(styleTr, "FontSize", "12pt"); // sample header font size
                AppendElement(styleTr, "FontWeight", "Bold");
                AppendElement(styleTr, "Color", "White");
                tr.AppendChild(styleTr);
                textRuns.AppendChild(tr);
                para.AppendChild(textRuns);
                para.AppendChild(CreateElement("Style"));
                paras.AppendChild(para);
                tb.AppendChild(paras);
                var tbStyle = CreateElement("Style");
                var border = CreateElement("Border");
                AppendElement(border, "Color", "LightGrey");
                AppendElement(border, "Style", "Solid");
                tbStyle.AppendChild(border);
                AppendElement(tbStyle, "BackgroundColor", "#305496"); // sample background
                AppendElement(tbStyle, "PaddingLeft", "2pt");
                AppendElement(tbStyle, "PaddingRight", "2pt");
                AppendElement(tbStyle, "PaddingTop", "2pt");
                AppendElement(tbStyle, "PaddingBottom", "2pt");
                tb.AppendChild(tbStyle);
                cellContents.AppendChild(tb);
                cell.AppendChild(cellContents);
                headerCells.AppendChild(cell);
            }
            headerRow.AppendChild(headerCells);
            rows.AppendChild(headerRow);

            // Detail row
            var detailRow = CreateElement("TablixRow");
            AppendElement(detailRow, "Height", "6mm");
            var detailCells = CreateElement("TablixCells");
            for (int i = 0; i < colCount; i++)
            {
                var cell = CreateElement("TablixCell");
                var cellContents = CreateElement("CellContents");
                var tb = CreateElement("Textbox");
                string fieldName = fieldsToShow.Count > i ? fieldsToShow[i].Name : $"Col{i+1}";
                tb.SetAttribute("Name", SanitizeName(fieldName));
                AppendElement(tb, "CanGrow", "true");
                AppendElement(tb, "KeepTogether", "true");
                var paras = CreateElement("Paragraphs");
                var para = CreateElement("Paragraph");
                var textRuns = CreateElement("TextRuns");
                var tr = CreateElement("TextRun");
                AppendElement(tr, "Value", $"=Fields!{SanitizeName(fieldName)}.Value");
                var styleTr = CreateElement("Style");
                AppendElement(styleTr, "FontFamily", "Calibri");
                AppendElement(styleTr, "FontSize", "11pt");
                tr.AppendChild(styleTr);
                textRuns.AppendChild(tr);
                para.AppendChild(textRuns);
                para.AppendChild(CreateElement("Style"));
                paras.AppendChild(para);
                tb.AppendChild(paras);
                var tbStyle = CreateElement("Style");
                var border = CreateElement("Border");
                AppendElement(border, "Color", "LightGrey");
                AppendElement(border, "Style", "Solid");
                tbStyle.AppendChild(border);
                AppendElement(tbStyle, "PaddingLeft", "2pt");
                AppendElement(tbStyle, "PaddingRight", "2pt");
                AppendElement(tbStyle, "PaddingTop", "2pt");
                AppendElement(tbStyle, "PaddingBottom", "2pt");
                tb.AppendChild(tbStyle);
                cellContents.AppendChild(tb);
                cell.AppendChild(cellContents);
                detailCells.AppendChild(cell);
            }
            detailRow.AppendChild(detailCells);
            rows.AppendChild(detailRow);

            body.AppendChild(columns);
            body.AppendChild(rows);
            tablix.AppendChild(body);

            // ColumnHierarchy
            var colHier = CreateElement("TablixColumnHierarchy");
            var colMembers = CreateElement("TablixMembers");
            for (int i = 0; i < colCount; i++)
            {
                colMembers.AppendChild(CreateElement("TablixMember"));
            }
            colHier.AppendChild(colMembers);
            tablix.AppendChild(colHier);

            // RowHierarchy (Header + Details)
            var rowHier = CreateElement("TablixRowHierarchy");
            var rowMembers = CreateElement("TablixMembers");
            if (_model.Parameters != null && _model.Parameters.Count > 0)
            {
                var mParams = CreateElement("TablixMember");
                AppendElement(mParams, "KeepWithGroup", "After");
                rowMembers.AppendChild(mParams);
            }
            var mHeader = CreateElement("TablixMember");
            AppendElement(mHeader, "KeepWithGroup", "After");
            rowMembers.AppendChild(mHeader);
            var mDetail = CreateElement("TablixMember");
            var grp = CreateElement("Group");
            grp.SetAttribute("Name", "Details");
            mDetail.AppendChild(grp);
            rowMembers.AppendChild(mDetail);
            rowHier.AppendChild(rowMembers);
            tablix.AppendChild(rowHier);

            // DataSetName and position/size
            if (_model.Tables != null && _model.Tables.Count > 0)
                AppendElement(tablix, "DataSetName", SanitizeName(_model.Tables[0].Alias ?? _model.Tables[0].Name));
            AppendElement(tablix, "Top", "0mm");
            AppendElement(tablix, "Left", "0mm");
            AppendElement(tablix, "Height", "43.49374mm"); // sample height
            AppendElement(tablix, "Width", "559.15mm");     // sample width

            var tblStyle = CreateElement("Style");
            var b = CreateElement("Border");
            AppendElement(b, "Style", "None");
            tblStyle.AppendChild(b);
            tablix.AppendChild(tblStyle);

            return tablix;
        }

        private XmlElement CreatePageSettings()
        {
            var page = CreateElement("Page");

            // Optional footer (empty)
            var footer = CreateElement("PageFooter");
            AppendElement(footer, "Height", "11.43mm");
            AppendElement(footer, "PrintOnFirstPage", "true");
            AppendElement(footer, "PrintOnLastPage", "true");
            var fs = CreateElement("Style");
            var fb = CreateElement("Border");
            AppendElement(fb, "Style", "None");
            fs.AppendChild(fb);
            footer.AppendChild(fs);
            page.AppendChild(footer);

            AppendElement(page, "PageHeight", PAGE_HEIGHT_MM);
            AppendElement(page, "PageWidth", PAGE_WIDTH_MM);
            AppendElement(page, "LeftMargin", MARGIN_MM);
            AppendElement(page, "RightMargin", MARGIN_MM);
            AppendElement(page, "TopMargin", MARGIN_MM);
            AppendElement(page, "BottomMargin", MARGIN_MM);
            AppendElement(page, "ColumnSpacing", "0.13cm");
            page.AppendChild(CreateElement("Style"));

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
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c); else sb.Append('_');
            }
            var result = sb.ToString();
            if (result.Length == 0) result = "Field";
            // Names in RDL cannot start with digits; prepend underscore if necessary
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }

        private string ConvertFieldClrType(string crystalDataType)
        {
            if (string.IsNullOrEmpty(crystalDataType)) return "System.String";
            var upper = crystalDataType.ToUpper();
            if (upper.Contains("VARCHAR") || upper.Contains("STRING") || upper.Contains("CHAR")) return "System.String";
            if (upper.Contains("NUMBER") || upper.Contains("DECIMAL")) return "System.Decimal";
            if (upper.Contains("INT")) return "System.Int32";
            if (upper.Contains("DATE") || upper.Contains("DATETIME") || upper.Contains("TIMESTAMP")) return "System.DateTime";
            if (upper.Contains("BOOLEAN") || upper.Contains("BIT")) return "System.Boolean";
            if (upper.Contains("FLOAT") || upper.Contains("DOUBLE")) return "System.Double";
            return "System.String";
        }

        private string ConvertParameterDataType(string crystalDataType)
        {
            if (string.IsNullOrEmpty(crystalDataType))
                return "String";
            var upper = crystalDataType.ToUpper();
            if (upper.Contains("VARCHAR") || upper.Contains("STRING") || upper.Contains("CHAR")) return "String";
            if (upper.Contains("NUMBER") || upper.Contains("DECIMAL")) return "Float";
            if (upper.Contains("INT")) return "Integer";
            if (upper.Contains("DOUBLE") || upper.Contains("FLOAT")) return "Float";
            if (upper.Contains("DATE") || upper.Contains("DATETIME") || upper.Contains("TIMESTAMP")) return "DateTime";
            if (upper.Contains("BOOLEAN") || upper.Contains("BIT")) return "Boolean";
            return "String";
        }

        private string FormatXml(XmlDocument doc)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = System.Text.Encoding.UTF8,
                OmitXmlDeclaration = true
            };
            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

        public void SaveToFile(string filePath)
        {
            var rdlContent = GenerateRDL();
            var encoding = new System.Text.UTF8Encoding(true);
            System.IO.File.WriteAllText(filePath, rdlContent, encoding);
        }
    }
}
