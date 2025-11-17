using System;
using Microsoft.Extensions.Logging;
using SSMSPlusFlowAnalyzer.Entities;
using SSMSPlusFlowAnalyzer.Parsers;

namespace SSMSPlusFlowAnalyzer.Services
{
    /// <summary>
    /// Service for analyzing SQL control flow
    /// </summary>
    public class FlowAnalysisService
    {
        private readonly SqlTextProvider _sqlTextProvider;
        private readonly ILogger<FlowAnalysisService> _logger;

        public FlowAnalysisService(
            SqlTextProvider sqlTextProvider,
            ILogger<FlowAnalysisService> logger)
        {
            _sqlTextProvider = sqlTextProvider;
            _logger = logger;
        }

        /// <summary>
        /// Analyzes the SQL from the active editor
        /// </summary>
        public SqlFlowAnalysisResult AnalyzeActiveDocument()
        {
            try
            {
                var sql = _sqlTextProvider.GetSqlText();

                if (string.IsNullOrWhiteSpace(sql))
                {
                    return new SqlFlowAnalysisResult
                    {
                        OriginalSql = "",
                        Errors = new System.Collections.Generic.List<string>
                        {
                            "No SQL text found in the active editor. Please open a SQL file or select some SQL text."
                        }
                    };
                }

                _logger.LogInformation($"Analyzing SQL text ({sql.Length} characters)");

                return AnalyzeSql(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing active document");
                return new SqlFlowAnalysisResult
                {
                    OriginalSql = "",
                    Errors = new System.Collections.Generic.List<string>
                    {
                        $"Error analyzing SQL: {ex.Message}"
                    }
                };
            }
        }

        /// <summary>
        /// Analyzes the provided SQL text
        /// </summary>
        public SqlFlowAnalysisResult AnalyzeSql(string sql)
        {
            try
            {
                _logger.LogInformation("Starting SQL flow analysis");

                var parser = new SqlFlowParser();
                var result = parser.Parse(sql);

                _logger.LogInformation($"Analysis complete: {result.GetSummary()}");

                if (result.Errors.Count > 0)
                {
                    _logger.LogWarning($"Analysis completed with {result.Errors.Count} errors");
                }

                if (result.Warnings.Count > 0)
                {
                    _logger.LogWarning($"Analysis completed with {result.Warnings.Count} warnings");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL flow analysis");
                return new SqlFlowAnalysisResult
                {
                    OriginalSql = sql,
                    Errors = new System.Collections.Generic.List<string>
                    {
                        $"Parser error: {ex.Message}"
                    }
                };
            }
        }
    }
}
