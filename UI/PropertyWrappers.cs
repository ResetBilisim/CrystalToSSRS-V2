using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using CrystalToSSRS.Models;
using CrystalToSSRS.Converters;

namespace CrystalToSSRS.UI
{
    // Properties for ReportObject
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ReportObjectProperties
    {
        private ReportObject _obj;
        
        public ReportObjectProperties(ReportObject obj)
        {
            _obj = obj;
        }
        
        [Category("General")]
        [DisplayName("Name")]
        [Description("Object name")]
        public string Name
        {
            get => _obj.Name;
            set => _obj.Name = value;
        }
        
        [Category("General")]
        [DisplayName("Type")]
        [Description("Object type")]
        [ReadOnly(true)]
        public string Type => _obj.Type;
        
        [Category("Position (Millimeters)")]
        [DisplayName("Left (mm)")]
        [Description("Distance from left edge in millimeters")]
        public double LeftMM
        {
            get => TwipsToMM(_obj.Left);
            set => _obj.Left = MMToTwips(value);
        }
        
        [Category("Position (Millimeters)")]
        [DisplayName("Top (mm)")]
        [Description("Distance from top edge in millimeters")]
        public double TopMM
        {
            get => TwipsToMM(_obj.Top);
            set => _obj.Top = MMToTwips(value);
        }
        
        [Category("Size (Millimeters)")]
        [DisplayName("Width (mm)")]
        [Description("Object width in millimeters")]
        public double WidthMM
        {
            get => TwipsToMM(_obj.Width);
            set => _obj.Width = MMToTwips(value);
        }
        
        [Category("Size (Millimeters)")]
        [DisplayName("Height (mm)")]
        [Description("Object height in millimeters")]
        public double HeightMM
        {
            get => TwipsToMM(_obj.Height);
            set => _obj.Height = MMToTwips(value);
        }
        
        [Category("Position (Twips)")]
        [DisplayName("Left (twips)")]
        [Description("Left position in Crystal Reports format")]
        public double Left
        {
            get => _obj.Left;
            set => _obj.Left = value;
        }
        
        [Category("Position (Twips)")]
        [DisplayName("Top (twips)")]
        [Description("Top position in Crystal Reports format")]
        public double Top
        {
            get => _obj.Top;
            set => _obj.Top = value;
        }
        
        [Category("Size (Twips)")]
        [DisplayName("Width (twips)")]
        [Description("Width in Crystal Reports format")]
        public double Width
        {
            get => _obj.Width;
            set => _obj.Width = value;
        }
        
        [Category("Size (Twips)")]
        [DisplayName("Height (twips)")]
        [Description("Height in Crystal Reports format")]
        public double Height
        {
            get => _obj.Height;
            set => _obj.Height = value;
        }
        
        [Category("Position (Pixels @ 96 DPI)")]
        [DisplayName("Left (px)")]
        [Description("Left position in pixels")]
        public double LeftPx
        {
            get => TwipsToPixels(_obj.Left);
            set => _obj.Left = PixelsToTwips(value);
        }
        
        [Category("Position (Pixels @ 96 DPI)")]
        [DisplayName("Top (px)")]
        [Description("Top position in pixels")]
        public double TopPx
        {
            get => TwipsToPixels(_obj.Top);
            set => _obj.Top = PixelsToTwips(value);
        }
        
        [Category("Size (Pixels @ 96 DPI)")]
        [DisplayName("Width (px)")]
        [Description("Width in pixels")]
        public double WidthPx
        {
            get => TwipsToPixels(_obj.Width);
            set => _obj.Width = PixelsToTwips(value);
        }
        
        [Category("Size (Pixels @ 96 DPI)")]
        [DisplayName("Height (px)")]
        [Description("Height in pixels")]
        public double HeightPx
        {
            get => TwipsToPixels(_obj.Height);
            set => _obj.Height = PixelsToTwips(value);
        }
        
        [Category("Content")]
        [DisplayName("Text")]
        [Description("Text content for text objects")]
        public string Text
        {
            get => _obj.Text;
            set => _obj.Text = value;
        }
        
        [Category("Content")]
        [DisplayName("Data Source")]
        [Description("Data source for field objects")]
        public string DataSource
        {
            get => _obj.DataSource;
            set => _obj.DataSource = value;
        }
        
        [Category("Font")]
        [DisplayName("Font")]
        [Description("Font properties of the object")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public FontInfo Font
        {
            get => _obj.Font;
            set => _obj.Font = value;
        }
        
        // Conversion helpers
        private double TwipsToMM(double twips)
        {
            // 1 inch = 1440 twips = 25.4 mm
            return Math.Round(twips / 1440.0 * 25.4, 2);
        }
        
        private double MMToTwips(double mm)
        {
            return Math.Round(mm / 25.4 * 1440.0, 2);
        }
        
        private double TwipsToPixels(double twips)
        {
            // 1 inch = 1440 twips = 96 pixels (at 96 DPI)
            return Math.Round(twips / 1440.0 * 96.0, 2);
        }
        
        private double PixelsToTwips(double pixels)
        {
            return Math.Round(pixels / 96.0 * 1440.0, 2);
        }
    }
    
    // Properties for Section
    public class SectionProperties
    {
        private ReportSection _section;
        
        public SectionProperties(ReportSection section)
        {
            _section = section;
        }
        
        [Category("General")]
        [DisplayName("Name")]
        [ReadOnly(true)]
        public string Name => _section.Name;
        
        [Category("General")]
        [DisplayName("Type")]
        [ReadOnly(true)]
        public string Kind => _section.Kind;
        
        [Category("Size (Millimeters)")]
        [DisplayName("Height (mm)")]
        public double HeightMM
        {
            get => Math.Round(_section.Height / 1440.0 * 25.4, 2);
            set => _section.Height = Math.Round(value / 25.4 * 1440.0, 2);
        }
        
        [Category("Size (Twips)")]
        [DisplayName("Height (twips)")]
        public double Height
        {
            get => _section.Height;
            set => _section.Height = value;
        }
        
        [Category("Size (Pixels @ 96 DPI)")]
        [DisplayName("Height (px)")]
        public double HeightPx
        {
            get => Math.Round(_section.Height / 1440.0 * 96.0, 2);
            set => _section.Height = Math.Round(value / 96.0 * 1440.0, 2);
        }
        
        [Category("Content")]
        [DisplayName("Object Count")]
        [ReadOnly(true)]
        public int ObjectCount => _section.Objects.Count;
    }
    
    // Properties for Formula
    public class FormulaProperties
    {
        private ReportFormula _formula;
        private FormulaToExpressionConverter _converter;
        
        public FormulaProperties(ReportFormula formula, FormulaToExpressionConverter converter)
        {
            _formula = formula;
            _converter = converter;
        }
        
        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _formula.Name;
            set => _formula.Name = value;
        }
        
        [Category("General")]
        [DisplayName("Data Type")]
        [ReadOnly(true)]
        public string DataType => _formula.DataType;
        
        [Category("Crystal Reports")]
        [DisplayName("Formula (Crystal)")]
        [Description("Formula in Crystal Reports format")]
        public string FormulaText
        {
            get => _formula.FormulaText;
            set => _formula.FormulaText = value;
        }
        
        [Category("SSRS")]
        [DisplayName("Expression (SSRS)")]
        [Description("Expression converted to SSRS format")]
        [ReadOnly(true)]
        public string SSRSExpression
        {
            get
            {
                try
                {
                    return _converter.ConvertFormula(_formula.FormulaText);
                }
                catch (Exception ex)
                {
                    return $"ERROR: {ex.Message}";
                }
            }
        }
    }
}
