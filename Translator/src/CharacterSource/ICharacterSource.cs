namespace PythonCSharpTranslator 
{
    public interface ICharacterSource
    {
        int GetLineNumber();
        int GetColumnNumber();
        char? GetChar();
    }
}