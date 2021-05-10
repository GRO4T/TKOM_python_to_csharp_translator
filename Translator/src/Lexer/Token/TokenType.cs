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
        
        AssignmentSymbol,
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
        EqualSymbol,
        NotEqualSymbol,
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