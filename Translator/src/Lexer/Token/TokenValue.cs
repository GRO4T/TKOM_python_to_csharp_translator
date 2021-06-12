using System;

namespace PythonCSharpTranslator 
{
    public class TokenValue
    {
        public object Value { get; private set; }
        public Type Type;
        

        public TokenValue()
        {
        }
        public TokenValue(int value)
        {
            SetInt(value);
        }

        public TokenValue(bool value)
        {
            SetBool(value);
        }

        public TokenValue(string value)
        {
            SetString(value);
        }

        public TokenValue(double value)
        {
            SetDouble(value);
        }

        public int GetInt()
        {
            return (int) Value;
        }
        public double GetDouble()
        {
            return (double) Value;
        }

        public string GetString()
        {
            return (string) Value;
        }

        public bool GetBool()
        {
            return (bool) Value;
        }

        public object GetObject()
        {
            return Value;
        }

        public void SetInt(int value)
        {
            Value = value;
            Type = value.GetType();
        }

        public void SetDouble(double value)
        {
            Value = value;
            Type = value.GetType();
        }

        public void SetString(string s)
        {
            Value = s;
            Type = s?.GetType();
        }

        public void SetBool(bool value)
        {
            Value = value;
            Type = value.GetType();
        }
        
        public void AddInt(int value)
        {
            Value = (int) Value + value;
        }

        public void AddDouble(double value)
        {
            Value = (double) Value + value;
        }

        public void ConcatString(string s)
        {
            Value = (string) Value + s;
            Type = typeof(string);
        }

        public void ConvertToDouble()
        {
            Value = Convert.ToDouble(Value);
            Type = typeof(double);
        }

        public override string ToString()
        {
            return Value?.ToString();
        }
    }
}