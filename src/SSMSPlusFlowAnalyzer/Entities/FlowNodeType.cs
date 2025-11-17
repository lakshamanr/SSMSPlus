namespace SSMSPlusFlowAnalyzer.Entities
{
    /// <summary>
    /// Represents the type of a control flow node in SQL
    /// </summary>
    public enum FlowNodeType
    {
        /// <summary>
        /// BEGIN...END block
        /// </summary>
        BeginEnd,

        /// <summary>
        /// IF statement
        /// </summary>
        If,

        /// <summary>
        /// ELSE clause
        /// </summary>
        Else,

        /// <summary>
        /// WHILE loop
        /// </summary>
        While,

        /// <summary>
        /// TRY block
        /// </summary>
        Try,

        /// <summary>
        /// CATCH block
        /// </summary>
        Catch,

        /// <summary>
        /// GOTO statement
        /// </summary>
        Goto,

        /// <summary>
        /// Label definition (target for GOTO)
        /// </summary>
        Label,

        /// <summary>
        /// RETURN statement
        /// </summary>
        Return,

        /// <summary>
        /// BREAK statement
        /// </summary>
        Break,

        /// <summary>
        /// CONTINUE statement
        /// </summary>
        Continue,

        /// <summary>
        /// Regular SQL statement
        /// </summary>
        Statement
    }
}
