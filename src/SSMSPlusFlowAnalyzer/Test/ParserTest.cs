using System;
using SSMSPlusFlowAnalyzer.Parsers;

namespace SSMSPlusFlowAnalyzer.Test
{
    /// <summary>
    /// Quick test to verify the parser works
    /// </summary>
    public class ParserTest
    {
        public static void RunTest()
        {
            string testSql = @"
BEGIN
    PRINT 'Hello';

    IF 1 = 1
    BEGIN
        PRINT 'True';
    END
END
";

            Console.WriteLine("Testing SQL Parser...");
            Console.WriteLine("Input SQL:");
            Console.WriteLine(testSql);
            Console.WriteLine();

            var parser = new SqlFlowParser();
            var result = parser.Parse(testSql);

            Console.WriteLine($"Total Nodes: {result.AllNodes.Count}");
            Console.WriteLine($"BEGIN/END Blocks: {result.BeginEndBlocks.Count}");
            Console.WriteLine($"IF Statements: {result.IfStatements.Count}");
            Console.WriteLine($"Errors: {result.Errors.Count}");
            Console.WriteLine($"Warnings: {result.Warnings.Count}");
            Console.WriteLine();

            if (result.Errors.Count > 0)
            {
                Console.WriteLine("ERRORS:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("All Nodes:");
            foreach (var node in result.AllNodes)
            {
                Console.WriteLine($"  [{node.Id}] {node.NodeType} - {node.DisplayName} (Line {node.LineNumber}, Level {node.NestingLevel})");
            }
            Console.WriteLine();

            if (result.RootNode != null)
            {
                Console.WriteLine("Tree Structure:");
                PrintTree(result.RootNode, 0);
            }
        }

        private static void PrintTree(Entities.FlowNode node, int indent)
        {
            string indentStr = new string(' ', indent * 2);
            Console.WriteLine($"{indentStr}{node.DisplayName} (Level {node.NestingLevel})");

            foreach (var child in node.Children)
            {
                PrintTree(child, indent + 1);
            }
        }
    }
}
