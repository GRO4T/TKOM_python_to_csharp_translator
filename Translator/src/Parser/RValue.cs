using System;

namespace PythonCSharpTranslator
{
    public class RValue
    {
        private object _value;
        public enum Type
        {
            FunCall,
            Value
        }

        public RValue(FunctionCall functionCall)
        {
            SetFunCall(functionCall);
        }

        public RValue(Token value)
        {
            SetValue(value);
        }

        public void SetFunCall(FunctionCall functionCall)
        {
            _value = functionCall;
        }

        public void SetValue(Token value)
        {
            _value = value;
        }

        public FunctionCall GetFunCall()
        {
            return (FunctionCall) _value;
        }

        public Token GetValue()
        {
            return (Token) _value;
        }

        public void SetLogicalExpression()
        {
             
        }

    }
}