
using System.Collections;
using static PythonCSharpTranslator.TokenType;

namespace PythonCSharpTranslator
{
    public class Token
    {
        public TokenType Type { get; set; }

        public TokenValue Value { get; set; }
        
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }

        public string ErrorDescription;

        public Token()
        {
            
        }

        public Token(TokenType type)
        {
            Type = type;
        }
        public Token(TokenType type, TokenValue value)
        {
            Type = type;
            Value = value;
        }

        public Token(TokenType type, TokenValue value, int lineNumber, int columnNumber)
        {
            Type = type;
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public override string ToString()
        {
            return $"{Type}:{Value}";
        }

        public bool IsConstantValue()
        {
            return ((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant})
                            .Contains(Type);
        }
        
        public bool IsParameter()
        {
            return ((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier})
                            .Contains(Type);
        }

        public bool IsUnaryOperator()
        {
            return ((IList) new[]
                {
                    EqualSymbol, NotEqualSymbol, GreaterThan, LessThan,
                    GreaterEqualThan, LessEqualThan, AndToken, OrToken,
                    Plus, Minus, Slash, Star
                })
                            .Contains(Type);
        }

        public bool IsArithmeticOperator()
        {
            return ((IList) new[]
                {
                    Plus, Minus, Slash, Star
                })
                            .Contains(Type);
        }
        
        public bool IsLogicalOperator()
        {
            return ((IList) new[]
                {
                    EqualSymbol, NotEqualSymbol, GreaterThan, LessThan,
                    GreaterEqualThan, LessEqualThan, AndToken, OrToken,
                    NotToken
                })
                            .Contains(Type);
        }

        public bool IsType()
        {
            return ((IList) new[] {IntToken, StrToken, FloatToken, BoolToken})
                            .Contains(Type);
        }
    }
}