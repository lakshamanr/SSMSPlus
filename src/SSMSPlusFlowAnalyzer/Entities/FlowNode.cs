using System.Collections.Generic;

namespace SSMSPlusFlowAnalyzer.Entities
{
    /// <summary>
    /// Represents a node in the SQL control flow tree
    /// </summary>
    public class FlowNode
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of control flow node
        /// </summary>
        public FlowNodeType NodeType { get; set; }

        /// <summary>
        /// The SQL text for this node
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Condition for IF, WHILE statements
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Label name for GOTO/Label nodes
        /// </summary>
        public string LabelName { get; set; }

        /// <summary>
        /// Start position in the original SQL text
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// End position in the original SQL text
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Line number where this node starts
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Nesting level (depth in the tree)
        /// </summary>
        public int NestingLevel { get; set; }

        /// <summary>
        /// Parent node (null for root)
        /// </summary>
        public FlowNode Parent { get; set; }

        /// <summary>
        /// Child nodes
        /// </summary>
        public List<FlowNode> Children { get; set; }

        /// <summary>
        /// Execution order index
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// For IF nodes, the corresponding ELSE node (if any)
        /// </summary>
        public FlowNode ElseNode { get; set; }

        /// <summary>
        /// For GOTO nodes, the target label node
        /// </summary>
        public FlowNode TargetLabel { get; set; }

        /// <summary>
        /// For TRY nodes, the corresponding CATCH node
        /// </summary>
        public FlowNode CatchNode { get; set; }

        public FlowNode()
        {
            Children = new List<FlowNode>();
        }

        /// <summary>
        /// Gets a display name for this node
        /// </summary>
        public string DisplayName
        {
            get
            {
                switch (NodeType)
                {
                    case FlowNodeType.BeginEnd:
                        return $"BEGIN...END (Level {NestingLevel})";
                    case FlowNodeType.If:
                        return $"IF {Condition}";
                    case FlowNodeType.Else:
                        return "ELSE";
                    case FlowNodeType.While:
                        return $"WHILE {Condition}";
                    case FlowNodeType.Try:
                        return "TRY";
                    case FlowNodeType.Catch:
                        return "CATCH";
                    case FlowNodeType.Goto:
                        return $"GOTO {LabelName}";
                    case FlowNodeType.Label:
                        return $"LABEL: {LabelName}";
                    case FlowNodeType.Return:
                        return "RETURN";
                    case FlowNodeType.Break:
                        return "BREAK";
                    case FlowNodeType.Continue:
                        return "CONTINUE";
                    case FlowNodeType.Statement:
                        return Text?.Length > 50 ? Text.Substring(0, 50) + "..." : Text;
                    default:
                        return NodeType.ToString();
                }
            }
        }
    }
}
