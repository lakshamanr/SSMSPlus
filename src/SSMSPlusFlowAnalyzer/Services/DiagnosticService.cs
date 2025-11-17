using System;
using System.Text;
using Microsoft.Extensions.Logging;
using SSMSPlusFlowAnalyzer.Entities;
using SSMSPlusFlowAnalyzer.Parsers;

namespace SSMSPlusFlowAnalyzer.Services
{
    /// <summary>
    /// Diagnostic service to help debug parsing issues
    /// </summary>
    public class DiagnosticService
    {
        private readonly ILogger<DiagnosticService> _logger;

        public DiagnosticService(ILogger<DiagnosticService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Run complete diagnostic on SQL parsing
        /// </summary>
        public string DiagnoseParsingIssue(string sql)
        {
            var report = new StringBuilder();
            report.AppendLine("=== FLOW ANALYZER DIAGNOSTIC REPORT ===");
            report.AppendLine();

            // Step 1: Check input
            report.AppendLine("STEP 1: Input SQL");
            report.AppendLine($"Length: {sql?.Length ?? 0} characters");
            report.AppendLine($"Is Null or Empty: {string.IsNullOrWhiteSpace(sql)}");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                report.AppendLine($"First 200 chars: {(sql.Length > 200 ? sql.Substring(0, 200) + "..." : sql)}");
            }
            report.AppendLine();

            if (string.IsNullOrWhiteSpace(sql))
            {
                report.AppendLine("ERROR: SQL is null or empty!");
                return report.ToString();
            }

            // Step 2: Tokenize
            report.AppendLine("STEP 2: Tokenization");
            try
            {
                var lexer = new SqlLexer(sql);
                var tokens = lexer.Tokenize();
                report.AppendLine($"Total tokens: {tokens.Count}");

                // Show first 20 tokens
                report.AppendLine("First 20 tokens:");
                for (int i = 0; i < Math.Min(20, tokens.Count); i++)
                {
                    var token = tokens[i];
                    report.AppendLine($"  [{i}] {token.Type}: '{token.Value}' (Line {token.Line})");
                }

                // Count keyword tokens
                int beginCount = tokens.Count(t => t.Type == SqlTokenType.Begin);
                int endCount = tokens.Count(t => t.Type == SqlTokenType.End);
                int ifCount = tokens.Count(t => t.Type == SqlTokenType.If);
                int whileCount = tokens.Count(t => t.Type == SqlTokenType.While);

                report.AppendLine();
                report.AppendLine($"Keyword counts in tokens:");
                report.AppendLine($"  BEGIN: {beginCount}");
                report.AppendLine($"  END: {endCount}");
                report.AppendLine($"  IF: {ifCount}");
                report.AppendLine($"  WHILE: {whileCount}");
            }
            catch (Exception ex)
            {
                report.AppendLine($"ERROR during tokenization: {ex.Message}");
                report.AppendLine($"Stack: {ex.StackTrace}");
                return report.ToString();
            }
            report.AppendLine();

            // Step 3: Parse
            report.AppendLine("STEP 3: Parsing");
            try
            {
                var parser = new SqlFlowParser();
                var result = parser.Parse(sql);

                report.AppendLine($"Parse completed successfully");
                report.AppendLine($"Total nodes in AllNodes: {result.AllNodes.Count}");
                report.AppendLine($"Root node exists: {result.RootNode != null}");
                if (result.RootNode != null)
                {
                    report.AppendLine($"Root node children count: {result.RootNode.Children.Count}");
                }
                report.AppendLine();

                // Node breakdown
                report.AppendLine("Node breakdown:");
                report.AppendLine($"  BEGIN/END blocks: {result.BeginEndBlocks.Count}");
                report.AppendLine($"  IF statements: {result.IfStatements.Count}");
                report.AppendLine($"  WHILE loops: {result.WhileLoops.Count}");
                report.AppendLine($"  TRY/CATCH blocks: {result.TryCatchBlocks.Count}");
                report.AppendLine($"  GOTO statements: {result.GotoStatements.Count}");
                report.AppendLine($"  Labels: {result.Labels.Count}");
                report.AppendLine();

                // List all nodes
                report.AppendLine("All nodes detail:");
                foreach (var node in result.AllNodes)
                {
                    report.AppendLine($"  [{node.Id}] {node.NodeType} '{node.DisplayName}' - Line {node.LineNumber}, Level {node.NestingLevel}, Parent: {node.Parent?.Id.ToString() ?? "NULL"}");
                }
                report.AppendLine();

                // Tree structure
                report.AppendLine("Tree structure:");
                if (result.RootNode != null)
                {
                    PrintNodeTree(result.RootNode, report, 0);
                }
                else
                {
                    report.AppendLine("  No root node!");
                }
                report.AppendLine();

                // Errors and warnings
                if (result.Errors.Count > 0)
                {
                    report.AppendLine("ERRORS:");
                    foreach (var error in result.Errors)
                    {
                        report.AppendLine($"  - {error}");
                    }
                    report.AppendLine();
                }

                if (result.Warnings.Count > 0)
                {
                    report.AppendLine("WARNINGS:");
                    foreach (var warning in result.Warnings)
                    {
                        report.AppendLine($"  - {warning}");
                    }
                    report.AppendLine();
                }

                // Summary
                report.AppendLine("SUMMARY:");
                report.AppendLine($"  {result.GetSummary()}");
            }
            catch (Exception ex)
            {
                report.AppendLine($"ERROR during parsing: {ex.Message}");
                report.AppendLine($"Stack: {ex.StackTrace}");
            }

            report.AppendLine();
            report.AppendLine("=== END OF DIAGNOSTIC REPORT ===");

            return report.ToString();
        }

        private void PrintNodeTree(FlowNode node, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 2);
            sb.AppendLine($"{indentStr}[{node.Id}] {node.DisplayName} (Level {node.NestingLevel}, {node.Children.Count} children)");

            foreach (var child in node.Children)
            {
                PrintNodeTree(child, sb, indent + 1);
            }
        }
    }
}
