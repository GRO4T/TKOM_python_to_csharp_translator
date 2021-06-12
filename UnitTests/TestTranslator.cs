using System;
using System.Collections.Generic;
using PythonCSharpTranslator;
using Xunit;
using static PythonCSharpTranslator.StatementType;

namespace Tests
{
    public class TestTranslator
    {
        [Theory]
        [InlineData(new []{AssignmentStatementType, FunctionDefType}, new []{FunctionDefType, AssignmentStatementType})]
        
        public void TestReorderStatements(StatementType[] inStatementTypes, StatementType[] outStatementTypes)
        {
            var inStatements = new List<Statement>();
            foreach (var statementType in inStatementTypes)
            {
                inStatements.Add(new Statement{Type = statementType});
            }
            Translator.ReorderStatements(ref inStatements);
            for (int i = 0; i < inStatements.Count; i++)
            {
                Assert.Equal(outStatementTypes[i], inStatements[i].Type); 
            }
        }
        
        [Fact]
        public void TestVariableDef()
        {
            var variableDef = new VariableDef
            {
                Name = "x", InitialValue = new Token(TokenType.IntegerConstant, new TokenValue(2)),
                VariableType = TokenType.IntToken
            };
            var program = new ProgramObject {Statements = new List<Statement>() {variableDef}};
            var sourceCode = Translator.Translate(program);
            Assert.Equal(SingleStatementProgram("int x = 2;"), sourceCode);
        }
        
        [Fact]
        public void TestAssignment()
        {
            var assignmentStatement = new AssignmentStatement() 
            {
                LeftSide = "x",
                RightSide = new RValue(new Token(TokenType.DecimalConstant, new TokenValue(2.5)))
            };
            var program = new ProgramObject {Statements = new List<Statement>() {assignmentStatement}};
            var sourceCode = Translator.Translate(program);
            Assert.Equal(SingleStatementProgram("x = 2.5;"), sourceCode);
        }
        
        [Fact]
        public void TestConstantDef()
        {
            var constantDef = new ConstantDef() 
            {
                Name = "x",
                RightSide = new RValue(new Token(TokenType.LogicalConstant, new TokenValue(false))),
                ConstantType = TokenType.BoolToken
            };
            var program = new ProgramObject {Statements = new List<Statement>() {constantDef}};
            var sourceCode = Translator.Translate(program);
            Assert.Equal(SingleStatementProgram("const bool x = false;"), sourceCode);
        }
        
        [Fact]
        public void TestIfStatement()
        {
            var variableDef = new VariableDef
            {
                Name = "x", InitialValue = new Token(TokenType.IntegerConstant, new TokenValue(2)),
                VariableType = TokenType.IntToken
            };
            var ifStatement = new IfStatement() 
            {
                Condition = new List<Token>()
                {
                    new Token(TokenType.LeftParenthesis, new TokenValue("(")),
                    new Token(TokenType.Identifier, new TokenValue("x")),
                    new Token(TokenType.EqualSymbol, new TokenValue("==")),
                    new Token(TokenType.IntegerConstant, new TokenValue(1)),
                    new Token(TokenType.RightParenthesis, new TokenValue(")"))
                },
                Statements = new List<Statement>() {variableDef}
            };
            var program = new ProgramObject {Statements = new List<Statement>() {ifStatement}};
            var sourceCode = Translator.Translate(program);
            Assert.Equal(SingleStatementProgram("if (x == 1)\n\t\t{\n\t\t\tint x = 2;\n\t\t}"), sourceCode);
        }
        
        
        [Fact]
        public void TestWhileLoop()
        {
            var variableDef = new VariableDef
            {
                Name = "x", InitialValue = new Token(TokenType.IntegerConstant, new TokenValue(2)),
                VariableType = TokenType.IntToken
            };
            var whileLoop = new WhileLoop() 
            {
                Condition = new List<Token>()
                {
                    new Token(TokenType.LeftParenthesis, new TokenValue("(")),
                    new Token(TokenType.Identifier, new TokenValue("x")),
                    new Token(TokenType.EqualSymbol, new TokenValue("==")),
                    new Token(TokenType.IntegerConstant, new TokenValue(1)),
                    new Token(TokenType.RightParenthesis, new TokenValue(")"))
                },
                Statements = new List<Statement>() {variableDef}
            };
            var program = new ProgramObject {Statements = new List<Statement>() {whileLoop}};
            var sourceCode = Translator.Translate(program);
            Assert.Equal(SingleStatementProgram("while (x == 1)\n\t\t{\n\t\t\tint x = 2;\n\t\t}"), sourceCode);
        }
        
        [Fact]
        public void TestForLoop()
        {
            var variableDef = new VariableDef
            {
                Name = "x", InitialValue = new Token(TokenType.IntegerConstant, new TokenValue(2)),
                VariableType = TokenType.IntToken
            };
            var forLoop = new ForLoop() 
            {
                Start = 0,
                End = 5,
                IteratorName = "i",
                Statements = new List<Statement>() {variableDef}
            };
            var program = new ProgramObject {Statements = new List<Statement>() {forLoop}};
            var sourceCode = Translator.Translate(program);
            Assert.Equal(SingleStatementProgram("for (int i = 0; i < 5; i++)\n\t\t{\n\t\t\tint x = 2;\n\t\t}"), sourceCode);
        }

        [Fact]
        public void TestFunctionCall()
        {
            var functionCall = new FunctionCall()
            {
                Name = "hello",
                Args = new List<Token>() {new Token(TokenType.Identifier, new TokenValue("x"))}
            };
            var program = new ProgramObject {Statements = new List<Statement>() {functionCall}};
            var sourceCode = Translator.Translate(program);
            Assert.Equal(SingleStatementProgram("hello(x);"), sourceCode);
        }

        [Fact]
        public void TestFunctionDef()
        {
            var variableDef = new VariableDef
            {
                Name = "x", InitialValue = new Token(TokenType.IntegerConstant, new TokenValue(2)),
                VariableType = TokenType.IntToken
            };
            var functionDef = new FunctionDef()
            {
                Name = "hello",
                ReturnType = TokenType.IntToken,
                ArgList = new List<Tuple<string, TokenType>>() {new Tuple<string, TokenType>("y", TokenType.IntToken)},
                Statements = new List<Statement>() {variableDef}
            };
            var program = new ProgramObject {Statements = new List<Statement>() {functionDef}};
            var sourceCode = Translator.Translate(program);
            string expectedProgram = "using System.IO;\nusing System;\n\ninternal static class Program\n{\n";
            expectedProgram += "\tstatic int hello(int y)\n\t{\n\t\tint x = 2;\n\t}";
            expectedProgram += "\n\tstatic void Main(string[] args)\n\t{\n\t}\n}\n";
            Assert.Equal(expectedProgram, sourceCode);
        }

        private static string SingleStatementProgram(string statement)
        {
            string program = "using System.IO;\nusing System;\n\ninternal static class Program\n";
            program += "{\n\tstatic void Main(string[] args)\n\t{\n";
            program += $"\t\t{statement}\n";
            program += "\t}\n}\n";
            return program;
        }
    }
}