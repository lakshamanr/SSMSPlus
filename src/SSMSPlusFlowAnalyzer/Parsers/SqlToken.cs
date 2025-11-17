namespace SSMSPlusFlowAnalyzer.Parsers
{
    /// <summary>
    /// Represents a SQL token
    /// </summary>
    public class SqlToken
    {
        public SqlTokenType Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public SqlToken(SqlTokenType type, string value, int position, int line, int column)
        {
            Type = type;
            Value = value;
            Position = position;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}: {Value} (Line {Line}, Col {Column})";
        }
    }

    /// <summary>
    /// SQL token types
    /// </summary>
    public enum SqlTokenType
    {
        // Keywords
        Begin,
        End,
        If,
        Else,
        While,
        Try,
        Catch,
        Goto,
        Return,
        Break,
        Continue,

        // Identifiers and literals
        Identifier,
        Number,
        String,

        // Operators and punctuation
        LeftParen,
        RightParen,
        Comma,
        Semicolon,
        Equals,
        NotEquals,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,

        // Other
        Comment,
        Whitespace,
        Colon,  // For labels
        EndOfFile,
        Unknown
    }
}
