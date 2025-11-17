using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SSMSPlusFlowAnalyzer.Entities;

namespace SSMSPlusFlowAnalyzer.Parsers
{
    /// <summary>
    /// Parser for SQL control flow constructs
    /// </summary>
    public class SqlFlowParser
    {
        private List<SqlToken> _tokens;
        private int _currentTokenIndex;
        private int _nodeIdCounter;
        private int _executionOrderCounter;
        private SqlFlowAnalysisResult _result;
        private Dictionary<string, FlowNode> _labels;

        public SqlFlowParser()
        {
            _labels = new Dictionary<string, FlowNode>(StringComparer.OrdinalIgnoreCase);
        }

        public SqlFlowAnalysisResult Parse(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return new SqlFlowAnalysisResult
                {
                    OriginalSql = sql,
                    Errors = new List<string> { "SQL text is empty" }
                };
            }

            try
            {
                _result = new SqlFlowAnalysisResult
                {
                    OriginalSql = sql
                };

                // Tokenize
                var lexer = new SqlLexer(sql);
                _tokens = lexer.Tokenize();
                _currentTokenIndex = 0;
                _nodeIdCounter = 0;
                _executionOrderCounter = 0;
                _labels.Clear();

                // Parse the root
                _result.RootNode = new FlowNode
                {
                    Id = GetNextNodeId(),
                    NodeType = FlowNodeType.BeginEnd,
                    Text = "ROOT",
                    NestingLevel = 0,
                    StartPosition = 0,
                    EndPosition = sql.Length,
                    LineNumber = 1
                };

                _result.AllNodes.Add(_result.RootNode);

                // Parse all statements
                ParseStatements(_result.RootNode, 1);

                // Resolve GOTO targets
                ResolveGotoTargets();

                // Validate
                ValidateStructure();

                return _result;
            }
            catch (Exception ex)
            {
                _result.Errors.Add($"Parser error: {ex.Message}");
                return _result;
            }
        }

        private void ParseStatements(FlowNode parent, int nestingLevel)
        {
            while (!IsAtEnd() && !CheckCurrentToken(SqlTokenType.End))
            {
                try
                {
                    var node = ParseStatement(parent, nestingLevel);
                    if (node != null)
                    {
                        parent.Children.Add(node);
                        _result.AllNodes.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    _result.Errors.Add($"Error parsing statement at line {CurrentToken?.Line}: {ex.Message}");
                    // Skip to next statement
                    AdvanceToNextStatement();
                }
            }
        }

        private FlowNode ParseStatement(FlowNode parent, int nestingLevel)
        {
            if (IsAtEnd()) return null;

            var token = CurrentToken;

            // Check for control flow keywords
            switch (token.Type)
            {
                case SqlTokenType.Begin:
                    return ParseBeginEnd(parent, nestingLevel);

                case SqlTokenType.If:
                    return ParseIf(parent, nestingLevel);

                case SqlTokenType.While:
                    return ParseWhile(parent, nestingLevel);

                case SqlTokenType.Try:
                    return ParseTry(parent, nestingLevel);

                case SqlTokenType.Goto:
                    return ParseGoto(parent, nestingLevel);

                case SqlTokenType.Return:
                    return ParseReturn(parent, nestingLevel);

                case SqlTokenType.Break:
                    return ParseBreak(parent, nestingLevel);

                case SqlTokenType.Continue:
                    return ParseContinue(parent, nestingLevel);

                default:
                    // Check for label (identifier followed by colon)
                    if (token.Type == SqlTokenType.Identifier && PeekNextToken()?.Type == SqlTokenType.Colon)
                    {
                        return ParseLabel(parent, nestingLevel);
                    }

                    // Regular statement
                    return ParseRegularStatement(parent, nestingLevel);
            }
        }

        private FlowNode ParseBeginEnd(FlowNode parent, int nestingLevel)
        {
            var beginToken = Consume(SqlTokenType.Begin);

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.BeginEnd,
                Text = "BEGIN...END",
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = beginToken.Position,
                LineNumber = beginToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            // Parse statements inside BEGIN...END
            ParseStatements(node, nestingLevel + 1);

            // Consume END
            if (!IsAtEnd() && CheckCurrentToken(SqlTokenType.End))
            {
                var endToken = Consume(SqlTokenType.End);
                node.EndPosition = endToken.Position;
            }
            else
            {
                _result.Errors.Add($"Missing END for BEGIN at line {beginToken.Line}");
            }

            return node;
        }

        private FlowNode ParseIf(FlowNode parent, int nestingLevel)
        {
            var ifToken = Consume(SqlTokenType.If);

            // Parse condition (everything until BEGIN or another keyword)
            var condition = ParseCondition();

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.If,
                Text = $"IF {condition}",
                Condition = condition,
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = ifToken.Position,
                LineNumber = ifToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            // Parse the IF body (could be BEGIN...END or single statement)
            var ifBody = ParseStatement(node, nestingLevel + 1);
            if (ifBody != null)
            {
                node.Children.Add(ifBody);
                _result.AllNodes.Add(ifBody);
            }

            // Check for ELSE
            if (!IsAtEnd() && CheckCurrentToken(SqlTokenType.Else))
            {
                var elseToken = Consume(SqlTokenType.Else);

                var elseNode = new FlowNode
                {
                    Id = GetNextNodeId(),
                    NodeType = FlowNodeType.Else,
                    Text = "ELSE",
                    Parent = parent,
                    NestingLevel = nestingLevel,
                    StartPosition = elseToken.Position,
                    LineNumber = elseToken.Line,
                    ExecutionOrder = GetNextExecutionOrder()
                };

                node.ElseNode = elseNode;

                // Parse the ELSE body
                var elseBody = ParseStatement(elseNode, nestingLevel + 1);
                if (elseBody != null)
                {
                    elseNode.Children.Add(elseBody);
                    _result.AllNodes.Add(elseBody);
                }

                parent.Children.Add(elseNode);
                _result.AllNodes.Add(elseNode);
            }

            return node;
        }

        private FlowNode ParseWhile(FlowNode parent, int nestingLevel)
        {
            var whileToken = Consume(SqlTokenType.While);

            // Parse condition
            var condition = ParseCondition();

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.While,
                Text = $"WHILE {condition}",
                Condition = condition,
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = whileToken.Position,
                LineNumber = whileToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            // Parse the WHILE body
            var body = ParseStatement(node, nestingLevel + 1);
            if (body != null)
            {
                node.Children.Add(body);
                _result.AllNodes.Add(body);
            }

            return node;
        }

        private FlowNode ParseTry(FlowNode parent, int nestingLevel)
        {
            var tryToken = Consume(SqlTokenType.Try);

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.Try,
                Text = "TRY",
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = tryToken.Position,
                LineNumber = tryToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            // Parse the TRY body (should be BEGIN...END)
            var tryBody = ParseStatement(node, nestingLevel + 1);
            if (tryBody != null)
            {
                node.Children.Add(tryBody);
                _result.AllNodes.Add(tryBody);
            }

            // CATCH should follow
            if (!IsAtEnd() && CheckCurrentToken(SqlTokenType.Catch))
            {
                var catchToken = Consume(SqlTokenType.Catch);

                var catchNode = new FlowNode
                {
                    Id = GetNextNodeId(),
                    NodeType = FlowNodeType.Catch,
                    Text = "CATCH",
                    Parent = parent,
                    NestingLevel = nestingLevel,
                    StartPosition = catchToken.Position,
                    LineNumber = catchToken.Line,
                    ExecutionOrder = GetNextExecutionOrder()
                };

                node.CatchNode = catchNode;

                // Parse the CATCH body
                var catchBody = ParseStatement(catchNode, nestingLevel + 1);
                if (catchBody != null)
                {
                    catchNode.Children.Add(catchBody);
                    _result.AllNodes.Add(catchBody);
                }

                parent.Children.Add(catchNode);
                _result.AllNodes.Add(catchNode);
            }
            else
            {
                _result.Errors.Add($"Missing CATCH for TRY at line {tryToken.Line}");
            }

            return node;
        }

        private FlowNode ParseGoto(FlowNode parent, int nestingLevel)
        {
            var gotoToken = Consume(SqlTokenType.Goto);

            // Next token should be the label name
            string labelName = "";
            if (!IsAtEnd() && CurrentToken.Type == SqlTokenType.Identifier)
            {
                labelName = Consume(SqlTokenType.Identifier).Value;
            }

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.Goto,
                Text = $"GOTO {labelName}",
                LabelName = labelName,
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = gotoToken.Position,
                LineNumber = gotoToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            return node;
        }

        private FlowNode ParseLabel(FlowNode parent, int nestingLevel)
        {
            var labelToken = Consume(SqlTokenType.Identifier);
            Consume(SqlTokenType.Colon);

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.Label,
                Text = $"{labelToken.Value}:",
                LabelName = labelToken.Value,
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = labelToken.Position,
                LineNumber = labelToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            // Register label
            if (!_labels.ContainsKey(labelToken.Value))
            {
                _labels[labelToken.Value] = node;
            }
            else
            {
                _result.Warnings.Add($"Duplicate label '{labelToken.Value}' at line {labelToken.Line}");
            }

            return node;
        }

        private FlowNode ParseReturn(FlowNode parent, int nestingLevel)
        {
            var returnToken = Consume(SqlTokenType.Return);

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.Return,
                Text = "RETURN",
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = returnToken.Position,
                LineNumber = returnToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            // Skip to semicolon or next statement
            AdvanceToNextStatement();

            return node;
        }

        private FlowNode ParseBreak(FlowNode parent, int nestingLevel)
        {
            var breakToken = Consume(SqlTokenType.Break);

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.Break,
                Text = "BREAK",
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = breakToken.Position,
                LineNumber = breakToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            return node;
        }

        private FlowNode ParseContinue(FlowNode parent, int nestingLevel)
        {
            var continueToken = Consume(SqlTokenType.Continue);

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.Continue,
                Text = "CONTINUE",
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = continueToken.Position,
                LineNumber = continueToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            return node;
        }

        private FlowNode ParseRegularStatement(FlowNode parent, int nestingLevel)
        {
            var startToken = CurrentToken;
            var sb = new StringBuilder();

            // Collect tokens until we hit a control flow keyword, semicolon, or END
            while (!IsAtEnd())
            {
                var token = CurrentToken;

                if (IsControlFlowKeyword(token.Type) || token.Type == SqlTokenType.End)
                {
                    break;
                }

                sb.Append(token.Value);
                sb.Append(" ");
                Advance();

                if (token.Type == SqlTokenType.Semicolon)
                {
                    break;
                }
            }

            var text = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var node = new FlowNode
            {
                Id = GetNextNodeId(),
                NodeType = FlowNodeType.Statement,
                Text = text,
                Parent = parent,
                NestingLevel = nestingLevel,
                StartPosition = startToken.Position,
                LineNumber = startToken.Line,
                ExecutionOrder = GetNextExecutionOrder()
            };

            return node;
        }

        private string ParseCondition()
        {
            var sb = new StringBuilder();

            // Collect tokens until BEGIN or another control flow keyword
            while (!IsAtEnd())
            {
                var token = CurrentToken;

                if (token.Type == SqlTokenType.Begin || IsControlFlowKeyword(token.Type))
                {
                    break;
                }

                sb.Append(token.Value);
                sb.Append(" ");
                Advance();
            }

            return sb.ToString().Trim();
        }

        private void ResolveGotoTargets()
        {
            foreach (var gotoNode in _result.AllNodes.Where(n => n.NodeType == FlowNodeType.Goto))
            {
                if (_labels.TryGetValue(gotoNode.LabelName, out FlowNode targetLabel))
                {
                    gotoNode.TargetLabel = targetLabel;
                }
                else
                {
                    _result.Warnings.Add($"GOTO at line {gotoNode.LineNumber} references undefined label '{gotoNode.LabelName}'");
                }
            }
        }

        private void ValidateStructure()
        {
            // Check for unmatched BEGIN/END
            int beginCount = _result.AllNodes.Count(n => n.NodeType == FlowNodeType.BeginEnd);

            // Check for orphaned ELSE
            var orphanedElse = _result.AllNodes.Where(n => n.NodeType == FlowNodeType.Else &&
                !_result.AllNodes.Any(i => i.NodeType == FlowNodeType.If && i.ElseNode == n));

            foreach (var orphan in orphanedElse)
            {
                _result.Warnings.Add($"ELSE at line {orphan.LineNumber} without matching IF");
            }
        }

        private bool IsControlFlowKeyword(SqlTokenType type)
        {
            return type == SqlTokenType.Begin ||
                   type == SqlTokenType.If ||
                   type == SqlTokenType.Else ||
                   type == SqlTokenType.While ||
                   type == SqlTokenType.Try ||
                   type == SqlTokenType.Catch ||
                   type == SqlTokenType.Goto ||
                   type == SqlTokenType.Return ||
                   type == SqlTokenType.Break ||
                   type == SqlTokenType.Continue;
        }

        private void AdvanceToNextStatement()
        {
            while (!IsAtEnd() && CurrentToken.Type != SqlTokenType.Semicolon && !IsControlFlowKeyword(CurrentToken.Type))
            {
                Advance();
            }

            if (!IsAtEnd() && CurrentToken.Type == SqlTokenType.Semicolon)
            {
                Advance();
            }
        }

        private SqlToken CurrentToken => _currentTokenIndex < _tokens.Count ? _tokens[_currentTokenIndex] : null;

        private SqlToken PeekNextToken()
        {
            int nextIndex = _currentTokenIndex + 1;
            return nextIndex < _tokens.Count ? _tokens[nextIndex] : null;
        }

        private SqlToken Consume(SqlTokenType expectedType)
        {
            var token = CurrentToken;
            if (token == null || token.Type != expectedType)
            {
                throw new Exception($"Expected {expectedType} but found {token?.Type} at line {token?.Line}");
            }
            Advance();
            return token;
        }

        private bool CheckCurrentToken(SqlTokenType type)
        {
            return CurrentToken?.Type == type;
        }

        private void Advance()
        {
            if (_currentTokenIndex < _tokens.Count)
            {
                _currentTokenIndex++;
            }
        }

        private bool IsAtEnd()
        {
            return CurrentToken == null || CurrentToken.Type == SqlTokenType.EndOfFile;
        }

        private int GetNextNodeId()
        {
            return _nodeIdCounter++;
        }

        private int GetNextExecutionOrder()
        {
            return _executionOrderCounter++;
        }
    }
}
