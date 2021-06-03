namespace PythonCSharpTranslator
{
    public interface ITokenSource
    {
        Token GetNextToken();
        int GetLineNumber();
    }
}