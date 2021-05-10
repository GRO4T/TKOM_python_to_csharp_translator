using System.Collections.Generic;
using System.Linq;
using PythonCSharpTranslator;
using Xunit;
using static PythonCSharpTranslator.StatementType;
using static PythonCSharpTranslator.TokenType;

namespace Tests
{
    public class TestParser
    {
        [Theory]
        [InlineData(AssignmentStatement, new[] { Identifier, AssignmentSymbol, IntegerConstant })]
        [InlineData(AssignmentStatement, new[] { Identifier, AssignmentSymbol, Identifier })]
        [InlineData(VariableDef, new[] { Identifier, AssignmentSymbol, IntegerType, LeftParenthesis, IntegerConstant, RightParenthesis })]
        [InlineData(VariableDef, new[] { Identifier, AssignmentSymbol, BooleanType, LeftParenthesis, LogicalConstant, RightParenthesis })]
        [InlineData(VariableDef, new[] { Identifier, AssignmentSymbol, DecimalType, LeftParenthesis, DecimalConstant, RightParenthesis })]
        [InlineData(VariableDef, new[] { Identifier, AssignmentSymbol, StringType, LeftParenthesis, StringLiteral, RightParenthesis })]
        [InlineData(FunctionCall, new[] { Identifier, LeftParenthesis, RightParenthesis })]
        [InlineData(FunctionCall, new[] { Identifier, LeftParenthesis, Identifier, RightParenthesis })]
        [InlineData(FunctionCall, new[] { Identifier, LeftParenthesis, IntegerConstant, Comma, StringLiteral, RightParenthesis })]
        public void ParseStatement(StatementType expectedStatement, TokenType[] tokenTypes)
        {
            var tokens = new List<Token>();
            foreach (var tokenType in tokenTypes)
            {
                tokens.Add(new Token(tokenType));
            }
            var parser = new Parser(new TokenSourceMock(tokens));
            var s = parser.GetNextStatement();
            Assert.Equal(s.Type, expectedStatement);
        }
    } 
}