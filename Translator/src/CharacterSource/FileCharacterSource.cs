using System.IO;

namespace PythonCSharpTranslator 
{
    public class FileCharacterSource : ICharacterSource
    {
        private StreamReader _reader;
        private int _lineNumber = 1;
        private int _columnNumber = -1;
        private char? _lastCharacter;
        
        public FileCharacterSource(string path)
        {
            _reader = new StreamReader(path);
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
            if (_reader.Peek() >= 0)
            {
                if (_lastCharacter == '\n')
                {
                    _columnNumber = -1;
                    _lineNumber++;
                }
                _columnNumber++;
                _lastCharacter = (char)_reader.Read();
                return _lastCharacter;
            }
            _lastCharacter = null;
            return null;
        }
    }
}