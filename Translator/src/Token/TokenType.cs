namespace PythonCSharpTranslator
{
    public enum TokenType
    {
        End,
        Identifier,
        Type,
        Value,        
        
        Assignment,
        Colon,
        Comma,
        LeftParenthesis,
        RightParenthesis,
        Return,
        
        Plus,
        Minus,
        Mult,
        Div,
        
        LessThan,
        GreaterThan,
        Equals,
        LessEqualThan,
        GreaterEqualThan,
        
        Not,
        And,
        Or,
        
        For,
        While,
        If,
        Def,
    }
}