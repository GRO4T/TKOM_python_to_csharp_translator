namespace PythonCSharpTranslator 
{
    public class StringCharacterSource : ICharacterSource
    {
        private string _source;
        private int _charIndex;
        private int _lineNumber = 1;
        private int _columnNumber = -1;
        private char? _lastCharacter;

        public StringCharacterSource(string value)
        {
            _source = value;
            _charIndex = 0;
        }


        public int GetLineNumber()
        {
            return _lineNumber;
        }

        public int GetColumnNumber()
        {
            return _columnNumber;
        }

        public char? GetChar()
        {
            if (_charIndex < _source.Length)
            {
                if (_lastCharacter == '\n')
                {
                    _columnNumber = -1;
                    _lineNumber++;
                }
                _columnNumber++;
                _lastCharacter = _source[_charIndex++];
                return _lastCharacter;
            }
            return null;
        }
    }
}