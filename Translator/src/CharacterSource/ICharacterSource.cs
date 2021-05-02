using System.IO;

namespace Translator
{
    public interface ICharacterSource
    {
        int GetLineNumber();
        int GetColumnNumber();
        char? GetChar();
    }
}