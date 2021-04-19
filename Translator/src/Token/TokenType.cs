namespace Translator.Token
{
    public enum TokenType
    {
        End,
        Identifier,
        
        Int,
        String,
        Bool,
        Float,
        
        Assignment,
        Colon,
        Comma,
        LeftParenthesis,
        RightParenthesis,
        Return,
        
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