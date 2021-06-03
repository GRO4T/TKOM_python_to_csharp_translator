using System;
using PythonCSharpTranslator;
using Xunit;

namespace Tests
{
    public class TestSemanticAnalyzer
    {
        [Theory]
        [InlineData("Resources/semantic/variable_not_declared.py")]
        [InlineData("Resources/semantic/constant_modified.py")]
        [InlineData("Resources/semantic/modify_function_def.py")]
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
        
        [Theory]
        [InlineData("Resources/semantic/correct_variable_use.py")]
        public void AnalyzeCorrectCode(string filename)
        {
            var semanticAnalyzer = new SemanticAnalyzer(
                new Parser(new Lexer(new FileCharacterSource(filename))));
            while (!semanticAnalyzer.IsEnd())
            {
                var s = semanticAnalyzer.AnalyzeNextStatement();
            }
        }
    
        [Fact]
        public void ConstantDefinition()
        {
            var semanticAnalyzer = new SemanticAnalyzer(
                new Parser(new Lexer(new FileCharacterSource("Resources/semantic/constant_def.py"))));
            var s = semanticAnalyzer.AnalyzeNextStatement();
            Assert.Equal(StatementType.ConstantDefType, s.Type);
        }
    }
}