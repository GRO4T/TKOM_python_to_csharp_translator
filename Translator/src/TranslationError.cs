﻿using System;

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

        public TranslationError(string msg, int lineNumber) : base($"{msg} at line:{lineNumber}")
        {
            
        }
    }
}