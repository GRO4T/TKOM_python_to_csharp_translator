namespace PythonCSharpTranslator.Exception
{
    public class PyCTException : System.Exception
    {
        public PyCTException(string message) : base(message)
        {
            
        }
    }

    public class TokenWrongValueTypeException : PyCTException
    {
        public TokenWrongValueTypeException(string message) : base(message)
        {
            
        }
    }
}