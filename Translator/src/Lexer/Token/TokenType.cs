namespace PythonCSharpTranslator 
{
    public enum TokenType
    {
        TabToken,
        NewlineToken,
        End,
        Identifier,
        
        IntToken,
        StrToken,
        BoolToken,
        FloatToken,
        
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
        
        NotToken,
        AndToken,
        OrToken,
        
        ForToken,
        WhileToken,
        IfToken,
        DefToken,
        InToken,
        RangeToken,
        
        LogicalConstant,
        DecimalConstant,
        IntegerConstant,
        StringLiteral,
        
        UnknownToken
    }
}