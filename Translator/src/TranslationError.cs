using System;

namespace PythonCSharpTranslator
{
    public class TranslationError : Exception
    {
        public TranslationError()
        {
            
        }
        public TranslationError(string msg) : base(msg)
        {
        }
    }
}