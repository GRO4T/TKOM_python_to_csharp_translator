using PythonCSharpTranslator;
using PythonCSharpTranslator.Exception;

namespace Translator.Token
{
    public class Token
    {
        public TokenType Type { get; set; }

        private object _value;

        public Token()
        {
            
        }

        public Token(TokenType type)
        {
            Type = type;
        }
        public Token(TokenType type, object value)
        {
            Type = type;
            Value = value;
        }

        public object Value
        {
            get => _value;
            set
            {
                var t = value.GetType();
                if (t == typeof(int))
                    _value = (int) value;
                else if (t == typeof(double))
                    _value = (double) value;
                else if (t == typeof(string))
                    _value = (string) value;
                else
                    throw new TokenWrongValueTypeException(
                        $"Wrong token value:{t}"
                    );

            }
        }
        
    }
}