using System.IO;

namespace Translator
{
    public interface ICharacterSource
    {
        char? GetChar();
    }
}