using System;

namespace Translator.Token
{
    public class TokenValue
    {
        private object _value;

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

        public int GetInt()
        {
            return (int) _value;
        }
        public double GetDouble()
        {
            return (double) _value;
        }

        public string GetString()
        {
            return (string) _value;
        }

        public bool GetBool()
        {
            return (bool) _value;
        }

        public void SetInt(int value)
        {
            _value = value;
        }

        public void SetDouble(double value)
        {
            _value = value;
        }

        public void SetString(string s)
        {
            _value = s;
        }

        public void SetBool(bool value)
        {
            _value = value;
        }
        
        public void AddInt(int value)
        {
            _value = (int) _value + value;
        }

        public void AddDouble(double value)
        {
            _value = (double) _value + value;
        }

        public void ConcatString(string s)
        {
            _value = (string) _value + s;
        }

        public void ConvertToDouble()
        {
            _value = Convert.ToDouble(_value);
        }
    }
}