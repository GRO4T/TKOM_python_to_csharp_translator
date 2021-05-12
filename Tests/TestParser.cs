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
        [InlineData(AssignmentStatementType, new[] { Identifier, AssignmentSymbol, IntegerConstant })]
        [InlineData(AssignmentStatementType, new[] { Identifier, AssignmentSymbol, Identifier })]
        [InlineData(VariableDefType, new[] { Identifier, AssignmentSymbol, IntegerType, LeftParenthesis, IntegerConstant, RightParenthesis })]
        [InlineData(VariableDefType, new[] { Identifier, AssignmentSymbol, BooleanType, LeftParenthesis, LogicalConstant, RightParenthesis })]
        [InlineData(VariableDefType, new[] { Identifier, AssignmentSymbol, DecimalType, LeftParenthesis, DecimalConstant, RightParenthesis })]
        [InlineData(VariableDefType, new[] { Identifier, AssignmentSymbol, StringType, LeftParenthesis, StringLiteral, RightParenthesis })]
        public void ParseStatement(StatementType expectedStatement, TokenType[] tokenTypes)
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
        public void ParseAssignment()
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
        public void ParseAssignmentWithFunction()
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
    
        [Theory]
        [InlineData("Resources/logical_expression1.py")]
        [InlineData("Resources/logical_expression2.py")]
        [InlineData("Resources/logical_expression3.py")]
        [InlineData("Resources/logical_expression4.py")]
        public void ParseAssignmentWithLogicalExpression(string filename)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            var s = parser.GetNextStatement();
            Assert.Equal(AssignmentStatementType, s.Type);
        }
        
        [Theory]
        [InlineData("Resources/logical_expression_bad1.py")]
        [InlineData("Resources/logical_expression_bad2.py")]
        [InlineData("Resources/logical_expression_bad3.py")]
        public void ParseAssignmentWithLogicalExpressionBad(string filename)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            var s = parser.GetNextStatement();
            Assert.Equal(BadStatementType, s.Type);
        }
        
        [Theory]
        [InlineData("Resources/if_statement.py")]
        public void ParseIfStatement(string filename)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            var s = parser.GetNextStatement();
            Assert.Equal(IfStatement, s.Type);
        }
        
        [Fact]
        public void ParseVariableDef()
        {
            var tokens = new List<Token>
            {
                new(Identifier, new TokenValue("hello")),
                new(AssignmentSymbol),
                new (IntegerType),
                new (LeftParenthesis),
                new(Identifier, new TokenValue("world")),
                new (RightParenthesis)
            };
            var parser = new Parser(new TokenSourceMock(tokens));
            var s = parser.GetNextStatement();
            Assert.Equal(s.Type, VariableDefType);
            var variableDef = (VariableDef) s;
            Assert.Equal("hello", variableDef.LeftSide.Value.GetString());
            Assert.Equal("world", variableDef.RightSide.Value.GetString());
            Assert.Equal(IntegerType, variableDef.VariableType);
        }
    
        [Theory]
        [InlineData(new [] {Identifier, LeftParenthesis, RightParenthesis}, new TokenType[0]{})]
        [InlineData(new [] {Identifier, LeftParenthesis, IntegerConstant, RightParenthesis}, new [] {IntegerConstant})]
        [InlineData(new [] {Identifier, LeftParenthesis, StringLiteral, Comma, DecimalConstant, RightParenthesis}, new [] {StringLiteral, DecimalConstant})]
        public void ParseFunCall(TokenType[] tokenTypes, TokenType[] args)
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
        
        
    } 
}