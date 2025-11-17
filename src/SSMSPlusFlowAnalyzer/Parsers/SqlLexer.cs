using System;
using System.Collections.Generic;
using System.Text;

namespace SSMSPlusFlowAnalyzer.Parsers
{
    /// <summary>
    /// Lexical analyzer for SQL
    /// </summary>
    public class SqlLexer
    {
        private readonly string _input;
        private int _position;
        private int _line;
        private int _column;

        private static readonly Dictionary<string, SqlTokenType> Keywords = new Dictionary<string, SqlTokenType>(StringComparer.OrdinalIgnoreCase)
        {
            { "BEGIN", SqlTokenType.Begin },
            { "END", SqlTokenType.End },
            { "IF", SqlTokenType.If },
            { "ELSE", SqlTokenType.Else },
            { "WHILE", SqlTokenType.While },
            { "TRY", SqlTokenType.Try },
            { "CATCH", SqlTokenType.Catch },
            { "GOTO", SqlTokenType.Goto },
            { "RETURN", SqlTokenType.Return },
            { "BREAK", SqlTokenType.Break },
            { "CONTINUE", SqlTokenType.Continue }
        };

        public SqlLexer(string input)
        {
            _input = input ?? string.Empty;
            _position = 0;
            _line = 1;
            _column = 1;
        }

        public List<SqlToken> Tokenize()
        {
            var tokens = new List<SqlToken>();

            while (!IsAtEnd())
            {
                var token = GetNextToken();
                if (token != null && token.Type != SqlTokenType.Whitespace && token.Type != SqlTokenType.Comment)
                {
                    tokens.Add(token);
                }
            }

            tokens.Add(new SqlToken(SqlTokenType.EndOfFile, "", _position, _line, _column));
            return tokens;
        }

        private SqlToken GetNextToken()
        {
            if (IsAtEnd()) return null;

            char current = Peek();

            // Skip whitespace
            if (char.IsWhiteSpace(current))
            {
                return ReadWhitespace();
            }

            // Comments
            if (current == '-' && PeekNext() == '-')
            {
                return ReadSingleLineComment();
            }

            if (current == '/' && PeekNext() == '*')
            {
                return ReadMultiLineComment();
            }

            // Strings
            if (current == '\'')
            {
                return ReadString();
            }

            // Numbers
            if (char.IsDigit(current))
            {
                return ReadNumber();
            }

            // Identifiers and keywords
            if (char.IsLetter(current) || current == '@' || current == '#' || current == '_')
            {
                return ReadIdentifierOrKeyword();
            }

            // Punctuation and operators
            return ReadPunctuation();
        }

        private SqlToken ReadWhitespace()
        {
            int start = _position;
            int startLine = _line;
            int startCol = _column;
            var sb = new StringBuilder();

            while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
            {
                sb.Append(Advance());
            }

            return new SqlToken(SqlTokenType.Whitespace, sb.ToString(), start, startLine, startCol);
        }

        private SqlToken ReadSingleLineComment()
        {
            int start = _position;
            int startLine = _line;
            int startCol = _column;
            var sb = new StringBuilder();

            while (!IsAtEnd() && Peek() != '\n')
            {
                sb.Append(Advance());
            }

            return new SqlToken(SqlTokenType.Comment, sb.ToString(), start, startLine, startCol);
        }

        private SqlToken ReadMultiLineComment()
        {
            int start = _position;
            int startLine = _line;
            int startCol = _column;
            var sb = new StringBuilder();

            // Consume /*
            sb.Append(Advance());
            sb.Append(Advance());

            while (!IsAtEnd())
            {
                if (Peek() == '*' && PeekNext() == '/')
                {
                    sb.Append(Advance());
                    sb.Append(Advance());
                    break;
                }
                sb.Append(Advance());
            }

            return new SqlToken(SqlTokenType.Comment, sb.ToString(), start, startLine, startCol);
        }

        private SqlToken ReadString()
        {
            int start = _position;
            int startLine = _line;
            int startCol = _column;
            var sb = new StringBuilder();

            // Consume opening quote
            sb.Append(Advance());

            while (!IsAtEnd())
            {
                char current = Peek();

                if (current == '\'')
                {
                    sb.Append(Advance());
                    // Check for escaped quote ''
                    if (!IsAtEnd() && Peek() == '\'')
                    {
                        sb.Append(Advance());
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    sb.Append(Advance());
                }
            }

            return new SqlToken(SqlTokenType.String, sb.ToString(), start, startLine, startCol);
        }

        private SqlToken ReadNumber()
        {
            int start = _position;
            int startLine = _line;
            int startCol = _column;
            var sb = new StringBuilder();

            while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == '.'))
            {
                sb.Append(Advance());
            }

            return new SqlToken(SqlTokenType.Number, sb.ToString(), start, startLine, startCol);
        }

        private SqlToken ReadIdentifierOrKeyword()
        {
            int start = _position;
            int startLine = _line;
            int startCol = _column;
            var sb = new StringBuilder();

            while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_' || Peek() == '@' || Peek() == '#'))
            {
                sb.Append(Advance());
            }

            string value = sb.ToString();

            // Check if it's a keyword
            if (Keywords.TryGetValue(value, out SqlTokenType keywordType))
            {
                return new SqlToken(keywordType, value, start, startLine, startCol);
            }

            return new SqlToken(SqlTokenType.Identifier, value, start, startLine, startCol);
        }

        private SqlToken ReadPunctuation()
        {
            int start = _position;
            int startLine = _line;
            int startCol = _column;
            char current = Peek();
            char next = PeekNext();

            // Two-character operators
            if (current == '<' && next == '=')
            {
                Advance();
                Advance();
                return new SqlToken(SqlTokenType.LessThanOrEqual, "<=", start, startLine, startCol);
            }
            if (current == '>' && next == '=')
            {
                Advance();
                Advance();
                return new SqlToken(SqlTokenType.GreaterThanOrEqual, ">=", start, startLine, startCol);
            }
            if (current == '<' && next == '>')
            {
                Advance();
                Advance();
                return new SqlToken(SqlTokenType.NotEquals, "<>", start, startLine, startCol);
            }
            if (current == '!' && next == '=')
            {
                Advance();
                Advance();
                return new SqlToken(SqlTokenType.NotEquals, "!=", start, startLine, startCol);
            }

            // Single-character operators
            char ch = Advance();
            SqlTokenType type;

            switch (ch)
            {
                case '(': type = SqlTokenType.LeftParen; break;
                case ')': type = SqlTokenType.RightParen; break;
                case ',': type = SqlTokenType.Comma; break;
                case ';': type = SqlTokenType.Semicolon; break;
                case '=': type = SqlTokenType.Equals; break;
                case '<': type = SqlTokenType.LessThan; break;
                case '>': type = SqlTokenType.GreaterThan; break;
                case '+': type = SqlTokenType.Plus; break;
                case '-': type = SqlTokenType.Minus; break;
                case '*': type = SqlTokenType.Multiply; break;
                case '/': type = SqlTokenType.Divide; break;
                case '%': type = SqlTokenType.Modulo; break;
                case ':': type = SqlTokenType.Colon; break;
                default: type = SqlTokenType.Unknown; break;
            }

            return new SqlToken(type, ch.ToString(), start, startLine, startCol);
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return _input[_position];
        }

        private char PeekNext()
        {
            if (_position + 1 >= _input.Length) return '\0';
            return _input[_position + 1];
        }

        private char Advance()
        {
            char current = _input[_position];
            _position++;
            _column++;

            if (current == '\n')
            {
                _line++;
                _column = 1;
            }

            return current;
        }

        private bool IsAtEnd()
        {
            return _position >= _input.Length;
        }
    }
}
