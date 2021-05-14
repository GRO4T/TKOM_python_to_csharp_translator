using System.Collections.Generic;
using PythonCSharpTranslator;
using Xunit;
using static PythonCSharpTranslator.StatementType;
using static PythonCSharpTranslator.TokenType;

namespace Tests
{
    public class TestParser
    {
        [Theory]
        [InlineData(AssignmentStatementType, new[] {Identifier, AssignmentSymbol, IntegerConstant})]
        [InlineData(AssignmentStatementType, new[] {Identifier, AssignmentSymbol, Identifier})]
        [InlineData(VariableDefType,
            new[] {Identifier, AssignmentSymbol, IntToken, LeftParenthesis, IntegerConstant, RightParenthesis})]
        [InlineData(VariableDefType,
            new[] {Identifier, AssignmentSymbol, BoolToken, LeftParenthesis, LogicalConstant, RightParenthesis})]
        [InlineData(VariableDefType,
            new[] {Identifier, AssignmentSymbol, FloatToken, LeftParenthesis, DecimalConstant, RightParenthesis})]
        [InlineData(VariableDefType,
            new[] {Identifier, AssignmentSymbol, StrToken, LeftParenthesis, StringLiteral, RightParenthesis})]
        public void ParseStatementFromTokens(StatementType expectedStatement, TokenType[] tokenTypes)
        {
            var tokens = new List<Token>();
            foreach (var tokenType in tokenTypes)
            {
                tokens.Add(new Token(tokenType));
            }

            var parser = new Parser(new TokenSourceMock(tokens));
            var s = parser.GetNextStatement();
            Assert.Equal(expectedStatement, s.Type);
        }

        [Fact]
        public void ParseAssignmentFromTokens()
        {
            var tokens = new List<Token>
            {
                new(Identifier, new TokenValue("hello")),
                new(AssignmentSymbol),
                new(IntegerConstant, new TokenValue(123))
            };
            var parser = new Parser(new TokenSourceMock(tokens));
            var s = parser.GetNextStatement();
            Assert.Equal(s.Type, AssignmentStatementType);
            var assignStatement = (AssignmentStatement) s;
            Assert.Equal("hello", assignStatement.LeftSide.Value.GetString());
            Assert.Equal(123, assignStatement.RightSide.GetValue().Value.GetInt());
        }

        [Fact]
        public void ParseAssignmentWithFunctionFromTokens()
        {
            var tokens = new List<Token>
            {
                new(Identifier, new TokenValue("hello")),
                new(AssignmentSymbol),
                new(Identifier, new TokenValue("hello_fun")),
                new Token(LeftParenthesis),
                new Token(RightParenthesis)
            };
            var parser = new Parser(new TokenSourceMock(tokens));
            var s = parser.GetNextStatement();
            Assert.Equal(s.Type, AssignmentStatementType);
            var assignStatement = (AssignmentStatement) s;
            Assert.Equal("hello", assignStatement.LeftSide.Value.GetString());
            Assert.Equal("hello_fun", assignStatement.RightSide.GetFunCall().Name);
        }

        [Fact]
        public void ParseVariableDefFromTokens()
        {
            var tokens = new List<Token>
            {
                new(Identifier, new TokenValue("hello")),
                new(AssignmentSymbol),
                new(IntToken),
                new(LeftParenthesis),
                new(Identifier, new TokenValue("world")),
                new(RightParenthesis)
            };
            var parser = new Parser(new TokenSourceMock(tokens));
            var s = parser.GetNextStatement();
            Assert.Equal(s.Type, VariableDefType);
            var variableDef = (VariableDef) s;
            Assert.Equal("hello", variableDef.LeftSide.Value.GetString());
            Assert.Equal("world", variableDef.RightSide.Value.GetString());
            Assert.Equal(IntToken, variableDef.VariableType);
        }

        [Theory]
        [InlineData(new[] {Identifier, LeftParenthesis, RightParenthesis}, new TokenType[0] { })]
        [InlineData(new[] {Identifier, LeftParenthesis, IntegerConstant, RightParenthesis}, new[] {IntegerConstant})]
        [InlineData(new[] {Identifier, LeftParenthesis, StringLiteral, Comma, DecimalConstant, RightParenthesis},
            new[] {StringLiteral, DecimalConstant})]
        public void ParseFunCallFromTokens(TokenType[] tokenTypes, TokenType[] args)
        {
            var tokens = new List<Token>();
            foreach (var tt in tokenTypes)
            {
                tokens.Add(new Token(tt));
            }

            tokens[0].Value = new TokenValue("hello");
            var parser = new Parser(new TokenSourceMock(tokens));
            var s = parser.GetNextStatement();
            Assert.Equal(FunctionCall, s.Type);
            var funCall = (FunCall) s;
            Assert.Equal(funCall.Args.Count, args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                Assert.Equal(args[i], funCall.Args[i].Type);
            }
        }

        [Theory]
        [InlineData("Resources/logical_expression1.py", AssignmentStatementType)]
        [InlineData("Resources/logical_expression2.py", AssignmentStatementType)]
        [InlineData("Resources/logical_expression3.py", AssignmentStatementType)]
        [InlineData("Resources/logical_expression4.py", AssignmentStatementType)]
        [InlineData("Resources/logical_expression_bad1.py", BadStatementType)]
        [InlineData("Resources/logical_expression_bad2.py", BadStatementType)]
        [InlineData("Resources/logical_expression_bad3.py", BadStatementType)]
        [InlineData("Resources/if_statement.py", IfStatement)]
        [InlineData("Resources/while_statement.py", WhileLoop)]
        [InlineData("Resources/for_loop.py", ForLoop)]
        public void ParseAssignmentWithLogicalExpression(string filename, StatementType expectedStatement)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            var s = parser.GetNextStatement();
            Assert.Equal(expectedStatement, s.Type);
        }
    }
}