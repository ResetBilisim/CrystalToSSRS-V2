using System.Collections.Generic;

namespace CrystalToSSRS.Models
{
    // Crystal Report yapısını temsil eden model
    public class CrystalReportModel
    {
        public string ReportName { get; set; }
        public OracleConnectionInfo ConnectionInfo { get; set; }
        public List<ReportSection> Sections { get; set; }
        public List<ReportParameter> Parameters { get; set; }
        public List<ReportFormula> Formulas { get; set; }
        public List<DatabaseTable> Tables { get; set; }
        public List<ReportField> Fields { get; set; }
        
        public CrystalReportModel()
        {
            Sections = new List<ReportSection>();
            Parameters = new List<ReportParameter>();
            Formulas = new List<ReportFormula>();
            Tables = new List<DatabaseTable>();
            Fields = new List<ReportField>();
        }
    }
    
    public class OracleConnectionInfo
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 1521;
        public string ServiceName { get; set; }
    }
    
    public class ReportSection
    {
        public string Name { get; set; }
        public string Kind { get; set; } // ReportHeader, PageHeader, Details, etc.
        public double Height { get; set; }
        public List<ReportObject> Objects { get; set; }
        
        public ReportSection()
        {
            Objects = new List<ReportObject>();
        }
    }
    
    public class ReportObject
    {
        public string Name { get; set; }
        public string Type { get; set; } // TextObject, FieldObject, LineObject, etc.
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Text { get; set; }
        public FontInfo Font { get; set; }
        public string DataSource { get; set; } // Field adı veya formula
    }
    
    public class FontInfo
    {
        public string Name { get; set; }
        public float Size { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
    }
    
    public class ReportParameter
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string PromptText { get; set; }
        public bool AllowMultipleValue { get; set; }
        public object DefaultValue { get; set; }
    }
    
    public class ReportFormula
    {
        public string Name { get; set; }
        public string FormulaText { get; set; }
        public string DataType { get; set; }
    }
    
    public class DatabaseTable
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public List<TableField> Fields { get; set; }
        
        public DatabaseTable()
        {
            Fields = new List<TableField>();
        }
    }
    
    public class TableField
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int Length { get; set; }
    }
    
    public class ReportField
    {
        public string Name { get; set; }
        public string TableName { get; set; }
        public string DataType { get; set; }
    }
}
