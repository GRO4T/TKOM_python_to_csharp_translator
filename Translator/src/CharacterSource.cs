using System.IO;

namespace Translator
{
    public class CharacterSource
    {
        private StreamReader _reader;

        public CharacterSource(string path)
        {
            _reader = new StreamReader(path);
        }

        public char? GetChar()
        {
            if (_reader.Peek() >= 0) 
                return (char) _reader.Read();
            return null;
        }
    }
}