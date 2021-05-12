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

        public RValue(FunCall funCall)
        {
            SetFunCall(funCall);
        }

        public RValue(Token value)
        {
            SetValue(value);
        }

        public void SetFunCall(FunCall funCall)
        {
            _value = funCall;
        }

        public void SetValue(Token value)
        {
            _value = value;
        }

        public FunCall GetFunCall()
        {
            return (FunCall) _value;
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