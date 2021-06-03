using System;
using System.Collections.Generic;

namespace PythonCSharpTranslator
{
    public class RValue
    {
        public RValueType Type = RValueType.Undefined;
        private object _value;
        public TokenType ValueType = TokenType.UnknownToken;
        public enum RValueType
        {
            FunCall,
            Value,
            LogicalExpression,
            ArithmeticExpression,
            Undefined
        }
        
        public RValue() {}

        public RValue(Token value)
        {
            SetValue(value);
        }

        public bool IsConstantValue()
        {
            return Type == RValueType.Value && GetValue().IsConstantValue();
        }

        public void SetFunCall(FunctionCall functionCall)
        {
            _value = functionCall;
            Type = RValueType.FunCall;
        }

        public void SetValue(Token value)
        {
            _value = value;
            Type = RValueType.Value;
        }

        public void SetArithmeticExpression(List<Token> tokens)
        {
            _value = tokens;
            Type = RValueType.ArithmeticExpression;
        }

        public void SetLogicalExpression(List<Token> tokens)
        {
            _value = tokens;
            Type = RValueType.LogicalExpression;
        }

        public FunctionCall GetFunCall()
        {
            return (FunctionCall) _value;
        }

        public Token GetValue()
        {
            return (Token) _value;
        }

        public List<Token> GetArithmeticExpression()
        {
            return (List<Token>) _value;
        }
        
        public List<Token> GetLogicalExpression()
        {
            return (List<Token>) _value;
        }
        
        

    }
}