using System.Collections.Generic;
using System.Linq;

namespace SSMSPlusFlowAnalyzer.Entities
{
    /// <summary>
    /// Represents the result of analyzing SQL control flow
    /// </summary>
    public class SqlFlowAnalysisResult
    {
        /// <summary>
        /// The original SQL text that was analyzed
        /// </summary>
        public string OriginalSql { get; set; }

        /// <summary>
        /// Root node of the control flow tree
        /// </summary>
        public FlowNode RootNode { get; set; }

        /// <summary>
        /// All nodes in the tree (flattened)
        /// </summary>
        public List<FlowNode> AllNodes { get; set; }

        /// <summary>
        /// All BEGIN...END blocks found
        /// </summary>
        public List<FlowNode> BeginEndBlocks => AllNodes?.Where(n => n.NodeType == FlowNodeType.BeginEnd).ToList();

        /// <summary>
        /// All IF statements found
        /// </summary>
        public List<FlowNode> IfStatements => AllNodes?.Where(n => n.NodeType == FlowNodeType.If).ToList();

        /// <summary>
        /// All WHILE loops found
        /// </summary>
        public List<FlowNode> WhileLoops => AllNodes?.Where(n => n.NodeType == FlowNodeType.While).ToList();

        /// <summary>
        /// All TRY/CATCH blocks found
        /// </summary>
        public List<FlowNode> TryCatchBlocks => AllNodes?.Where(n => n.NodeType == FlowNodeType.Try).ToList();

        /// <summary>
        /// All GOTO statements found
        /// </summary>
        public List<FlowNode> GotoStatements => AllNodes?.Where(n => n.NodeType == FlowNodeType.Goto).ToList();

        /// <summary>
        /// All label definitions found
        /// </summary>
        public List<FlowNode> Labels => AllNodes?.Where(n => n.NodeType == FlowNodeType.Label).ToList();

        /// <summary>
        /// Execution order of all nodes
        /// </summary>
        public List<FlowNode> ExecutionOrder => AllNodes?.OrderBy(n => n.ExecutionOrder).ToList();

        /// <summary>
        /// Maximum nesting level found
        /// </summary>
        public int MaxNestingLevel => AllNodes?.Any() == true ? AllNodes.Max(n => n.NestingLevel) : 0;

        /// <summary>
        /// Total number of control flow constructs
        /// </summary>
        public int TotalControlFlowCount => AllNodes?.Count(n => n.NodeType != FlowNodeType.Statement) ?? 0;

        /// <summary>
        /// Any parsing errors encountered
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Any warnings during analysis
        /// </summary>
        public List<string> Warnings { get; set; }

        public SqlFlowAnalysisResult()
        {
            AllNodes = new List<FlowNode>();
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        /// <summary>
        /// Gets a summary of the analysis
        /// </summary>
        public string GetSummary()
        {
            return $"Total Nodes: {AllNodes.Count}, " +
                   $"BEGIN/END: {BeginEndBlocks.Count}, " +
                   $"IF: {IfStatements.Count}, " +
                   $"WHILE: {WhileLoops.Count}, " +
                   $"TRY/CATCH: {TryCatchBlocks.Count}, " +
                   $"GOTO: {GotoStatements.Count}, " +
                   $"Max Nesting: {MaxNestingLevel}";
        }
    }
}
