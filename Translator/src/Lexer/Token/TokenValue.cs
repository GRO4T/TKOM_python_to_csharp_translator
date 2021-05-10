using System;

namespace PythonCSharpTranslator 
{
    public class TokenValue
    {
        public object Value { get; private set; }

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
        }

        public void SetDouble(double value)
        {
            Value = value;
        }

        public void SetString(string s)
        {
            Value = s;
        }

        public void SetBool(bool value)
        {
            Value = value;
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
        }

        public void ConvertToDouble()
        {
            Value = Convert.ToDouble(Value);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}