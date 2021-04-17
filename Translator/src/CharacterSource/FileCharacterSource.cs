using System.IO;

namespace Translator
{
    public class FileCharacterSource : ICharacterSource
    {
        private StreamReader _reader;
        
        public FileCharacterSource(string path)
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