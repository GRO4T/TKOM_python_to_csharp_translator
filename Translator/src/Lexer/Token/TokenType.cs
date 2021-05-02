namespace PythonCSharpTranslator 
{
    public enum TokenType
    {
        Indent,
        Newline,
        End,
        Identifier,
        
        IntegerType,
        StringType,
        BooleanType,
        DecimalType,
        
        Assignment,
        Colon,
        Comma,
        LeftParenthesis,
        RightParenthesis,
        Return,
        Arrow,
        
        Plus,
        Minus,
        Star,
        Slash,
        
        LessThan,
        GreaterThan,
        Equals,
        NotEquals,
        LessEqualThan,
        GreaterEqualThan,
        
        Not,
        And,
        Or,
        
        For,
        While,
        If,
        Def,
        
        LogicalConstant,
        DecimalConstant,
        IntegerConstant,
        StringLiteral,
        
        Unknown
    }
}