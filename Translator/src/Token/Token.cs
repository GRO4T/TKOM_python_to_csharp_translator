using System;
using PythonCSharpTranslator.Exception;

namespace PythonCSharpTranslator
{
    public class Token
    {
        public TokenType Type { get; set; }

        private object _value;
        
        public object Value
        {
            get { return _value;  }
            set
            {
                var t = value.GetType();
                if (t.Equals(typeof(int)))
                    _value = (int) value;
                else if (t.Equals(typeof(double)))
                    _value = (double) value;
                else if (t.Equals(typeof(string)))
                    _value = (string) value;
                else
                    throw new TokenWrongValueTypeException(
                        $"Wrong token value:{t}"
                    );

            }
        }
        
    }
}