namespace Translator.CharacterSource
{
    public class StringCharacterSource : ICharacterSource
    {
        private string _source;
        private int _charIndex;

        public StringCharacterSource(string value)
        {
            _source = value;
            _charIndex = 0;
        }


        public char? GetChar()
        {
            if (_charIndex < _source.Length)
                return _source[_charIndex++];
            return null;
        }
    }
}