using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using CrystalToSSRS.Models;

namespace CrystalToSSRS.Utils
{
    public class SqlParameterConversionResult
    {
        public string ConvertedSql { get; set; }
        public HashSet<string> SqlParams { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> MissingInModel { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> UnusedInSql { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public static class SqlParameterConverter
    {
        private static readonly Regex CrystalParam = new Regex(@"\{\?([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);
        private static readonly Regex AtParam = new Regex(@"@([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
        private static readonly Regex ColonParam = new Regex(@":([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        // Transform only outside of string literals
        private static string TransformOutsideStrings(string sql, Func<string, string> transformOutside)
        {
            if (string.IsNullOrEmpty(sql)) return sql;
            var sb = new System.Text.StringBuilder(sql.Length);
            int i = 0;
            while (i < sql.Length)
            {
                if (sql[i] == '\'')
                {
                    // Copy string literal ('' escape handled)
                    sb.Append('\''); i++;
                    while (i < sql.Length)
                    {
                        sb.Append(sql[i]);
                        if (sql[i] == '\'')
                        {
                            i++;
                            if (i < sql.Length && sql[i] == '\'')
                            {
                                sb.Append('\''); i++;
                                continue;
                            }
                            break;
                        }
                        i++;
                    }
                }
                else
                {
                    int start = i;
                    while (i < sql.Length && sql[i] != '\'') i++;
                    var segment = sql.Substring(start, i - start);
                    sb.Append(transformOutside(segment));
                }
            }
            return sb.ToString();
        }

        public static string ConvertToOracleColon(string sql)
        {
            return TransformOutsideStrings(sql, segment =>
            {
                // 1) Crystal: {?Param} -> :Param
                segment = CrystalParam.Replace(segment, m => ":" + m.Groups[1].Value);
                // 2) T-SQL: @Param -> :Param
                segment = AtParam.Replace(segment, m => ":" + m.Groups[1].Value);
                // 3) :Param already OK -> keep
                return segment;
            });
        }

        public static HashSet<string> ExtractOracleParamNames(string sql)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TransformOutsideStrings(sql, segment =>
            {
                foreach (Match m in ColonParam.Matches(segment))
                    set.Add(m.Groups[1].Value);
                return segment;
            });
            return set;
        }

        public static SqlParameterConversionResult ConvertAndValidate(string sql, IEnumerable<ReportParameter> modelParams)
        {
            var result = new SqlParameterConversionResult();
            result.ConvertedSql = ConvertToOracleColon(sql);
            var sqlParams = ExtractOracleParamNames(result.ConvertedSql);
            result.SqlParams = sqlParams;

            var modelSet = new HashSet<string>(
                modelParams?.Select(p => p.Name) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var p in sqlParams)
                if (!modelSet.Contains(p))
                    result.MissingInModel.Add(p);

            foreach (var p in modelSet)
                if (!sqlParams.Contains(p))
                    result.UnusedInSql.Add(p);

            return result;
        }

        public static SqlParameterConversionResult ConvertCommand(IDbCommand command, IEnumerable<ReportParameter> modelParams)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var res = ConvertAndValidate(command.CommandText, modelParams);
            command.CommandText = res.ConvertedSql;
            return res;
        }
    }
}
