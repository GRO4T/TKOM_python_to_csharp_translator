using System;
using PythonCSharpTranslator;
using Xunit;

namespace Tests
{
    public class TestSemanticAnalyzer
    {
        [Theory]
        [InlineData("Resources/logical_expression1.py")]
        public void AnalyzeBadCode(string filename)
        {
            var semanticAnalyzer = new SemanticAnalyzer(
                new Parser(new Lexer(new FileCharacterSource(filename))));

            Assert.Throws<TranslationError>(() =>
            {
                while (!semanticAnalyzer.IsEnd())
                {
                    var s = semanticAnalyzer.AnalyzeNextStatement();
                }
            });
        }
    }
}