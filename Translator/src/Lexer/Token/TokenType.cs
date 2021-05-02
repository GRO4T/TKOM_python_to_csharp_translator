namespace Translator.Token
{
    public enum TokenType
    {
        Indent,
        Newline,
        End,
        Identifier,
        
        IntType,
        StringType,
        BoolType,
        FloatType,
        
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
        IntConstant,
        StringLiteral,
        
        Unknown
    }
}