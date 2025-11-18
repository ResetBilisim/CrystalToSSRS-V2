using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CrystalToSSRS.Converters
{
    public class FormulaToExpressionConverter
    {
        // Crystal Reports fonksiyonlarını SSRS Expression'a çevir
        private static readonly Dictionary<string, string> FunctionMappings = new Dictionary<string, string>
        {
            // String fonksiyonları
            { "ToText", "CStr" },
            { "UpperCase", "UCase" },
            { "LowerCase", "LCase" },
            { "Trim", "Trim" },
            { "Left", "Left" },
            { "Right", "Right" },
            { "Mid", "Mid" },
            { "Length", "Len" },
            { "InStr", "InStr" },
            
            // Numeric fonksiyonları
            { "ToNumber", "CDbl" },
            { "Round", "Math.Round" },
            { "Truncate", "Math.Truncate" },
            { "Abs", "Math.Abs" },
            { "Ceiling", "Math.Ceiling" },
            { "Floor", "Math.Floor" },
            
            // Date fonksiyonları
            { "Year", "Year" },
            { "Month", "Month" },
            { "Day", "Day" },
            { "Hour", "Hour" },
            { "Minute", "Minute" },
            { "Second", "Second" },
            { "CurrentDate", "Today" },
            { "CurrentDateTime", "Now" },
            
            // Aggregate fonksiyonları
            { "Sum", "Sum" },
            { "Average", "Avg" },
            { "Count", "Count" },
            { "Maximum", "Max" },
            { "Minimum", "Min" },
            
            // Conditional
            { "IsNull", "IsNothing" }
        };
        
        public string ConvertFormula(string crystalFormula)
        {
            if (string.IsNullOrWhiteSpace(crystalFormula))
                return string.Empty;
                
            string expression = crystalFormula;
            
            // 1. Yorum satırlarını temizle
            expression = RemoveComments(expression);
            
            // 2. Field referanslarını çevir {Table.Field} -> Fields!Field.Value
            expression = ConvertFieldReferences(expression);
            
            // 3. Parameter referanslarını çevir {?ParameterName} -> Parameters!ParameterName.Value
            expression = ConvertParameterReferences(expression);
            
            // 4. Crystal fonksiyonlarını SSRS fonksiyonlarına çevir
            expression = ConvertFunctions(expression);
            
            // 5. If-Then-Else yapılarını çevir
            expression = ConvertIfThenElse(expression);
            
            // 6. Operatörleri çevir
            expression = ConvertOperators(expression);
            
            // 7. String birleştirmeleri çevir (& -> +)
            expression = expression.Replace(" & ", " + ");
            
            return "=" + expression.Trim();
        }
        
        private string RemoveComments(string formula)
        {
            // Crystal Reports'ta // ile başlayan yorumlar
            var lines = formula.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var cleanedLines = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!trimmedLine.StartsWith("//"))
                {
                    cleanedLines.Add(line);
                }
            }
            
            return string.Join(" ", cleanedLines);
        }
        
        private string ConvertFieldReferences(string expression)
        {
            // {TableName.FieldName} -> Fields!FieldName.Value
            var pattern = @"\{([^}\.]+)\.([^}]+)\}";
            expression = Regex.Replace(expression, pattern, match =>
            {
                var fieldName = match.Groups[2].Value;
                return $"Fields!{fieldName}.Value";
            });
            
            return expression;
        }
        
        private string ConvertParameterReferences(string expression)
        {
            // {?ParameterName} -> Parameters!ParameterName.Value
            var pattern = @"\{\?([^}]+)\}";
            expression = Regex.Replace(expression, pattern, match =>
            {
                var paramName = match.Groups[1].Value;
                return $"Parameters!{paramName}.Value";
            });
            
            return expression;
        }
        
        private string ConvertFunctions(string expression)
        {
            foreach (var mapping in FunctionMappings)
            {
                // Case-insensitive replace
                var pattern = $@"\b{Regex.Escape(mapping.Key)}\b";
                expression = Regex.Replace(expression, pattern, mapping.Value, RegexOptions.IgnoreCase);
            }
            
            return expression;
        }
        
        private string ConvertIfThenElse(string expression)
        {
            // Crystal: If condition Then value1 Else value2
            // SSRS: IIF(condition, value1, value2)
            
            var pattern = @"If\s+(.+?)\s+Then\s+(.+?)\s+Else\s+(.+?)(?=\s+If|\s*$)";
            expression = Regex.Replace(expression, pattern, match =>
            {
                var condition = match.Groups[1].Value.Trim();
                var trueValue = match.Groups[2].Value.Trim();
                var falseValue = match.Groups[3].Value.Trim();
                
                return $"IIF({condition}, {trueValue}, {falseValue})";
            }, RegexOptions.IgnoreCase);
            
            return expression;
        }
        
        private string ConvertOperators(string expression)
        {
            // Crystal'daki karşılaştırma operatörleri
            expression = Regex.Replace(expression, @"\bAnd\b", "AND", RegexOptions.IgnoreCase);
            expression = Regex.Replace(expression, @"\bOr\b", "OR", RegexOptions.IgnoreCase);
            expression = Regex.Replace(expression, @"\bNot\b", "NOT", RegexOptions.IgnoreCase);
            expression = Regex.Replace(expression, @"\bMod\b", "Mod", RegexOptions.IgnoreCase);
            
            return expression;
        }
        
        // Format string'leri çevir
        public string ConvertFormatString(string crystalFormat)
        {
            if (string.IsNullOrWhiteSpace(crystalFormat))
                return string.Empty;
                
            // Crystal format kodlarını .NET format kodlarına çevir
            var formatMappings = new Dictionary<string, string>
            {
                { "dd/MM/yyyy", "dd/MM/yyyy" },
                { "MM/dd/yyyy", "MM/dd/yyyy" },
                { "#,##0.00", "N2" },
                { "#,##0", "N0" },
                { "0.00%", "P2" },
                { "Currency", "C2" }
            };
            
            foreach (var mapping in formatMappings)
            {
                if (crystalFormat.Contains(mapping.Key))
                    return mapping.Value;
            }
            
            return crystalFormat;
        }
    }
}
