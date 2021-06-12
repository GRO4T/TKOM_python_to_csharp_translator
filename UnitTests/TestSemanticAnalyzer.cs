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
        [InlineData("Resources/semantic/symbol_already_declared.py")]
        [InlineData("Resources/semantic/variable_wrong_assignment.py")]
        [InlineData("Resources/semantic/negate_non_boolean.py")]
        [InlineData("Resources/semantic/function_used_as_variable.py")]
        [InlineData("Resources/semantic/logical_expression_ops_not_bool.py")]
        [InlineData("Resources/semantic/arithmetic_expression_ops_not_same_type.py")]
        public void AnalyzeBadCode(string filename)
        {
            var semanticAnalyzer = new SemanticAnalyzer(
                new Parser(new Lexer(new FileCharacterSource(filename))));

            Assert.Throws<TranslationError>(() =>
            {
                while (!semanticAnalyzer.IsEnd())
                {
                    var s = semanticAnalyzer.EvaluateNextStatement();
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
                var s = semanticAnalyzer.EvaluateNextStatement();
            }
        }
    
        [Fact]
        public void ConstantDefinition()
        {
            var semanticAnalyzer = new SemanticAnalyzer(
                new Parser(new Lexer(new FileCharacterSource("Resources/semantic/constant_def.py"))));
            var s = semanticAnalyzer.EvaluateNextStatement();
            Assert.Equal(StatementType.ConstantDefType, s.Type);
        }
    }
}