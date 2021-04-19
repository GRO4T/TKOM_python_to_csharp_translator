using System;
using System.Runtime.InteropServices;
using Translator;
using Translator.CharacterSource;
using Translator.Token;
using Xunit;

namespace Tests
{
    public class TestLexer
    {
        [Theory]
        [InlineData("", TokenType.End)]
        [InlineData("int", TokenType.Int)]
        [InlineData("str", TokenType.String)]
        [InlineData("float", TokenType.Float)]
        [InlineData("bool", TokenType.Bool)]
        [InlineData("(", TokenType.LeftParenthesis)]
        [InlineData(")", TokenType.RightParenthesis)]
        [InlineData(",", TokenType.Comma)]
        [InlineData(":", TokenType.Colon)]
        [InlineData("=", TokenType.Assignment)]
        [InlineData("+", TokenType.Plus)]
        [InlineData("-", TokenType.Minus)]
        [InlineData("*", TokenType.Star)]
        [InlineData("/", TokenType.Slash)]
        [InlineData("return", TokenType.Return)]
        [InlineData("<", TokenType.LessThan)]
        [InlineData(">", TokenType.GreaterThan)]
        [InlineData("==", TokenType.Equals)]
        [InlineData("!=", TokenType.NotEquals)]
        [InlineData("<=", TokenType.LessEqualThan)]
        [InlineData(">=", TokenType.GreaterEqualThan)]
        [InlineData("not", TokenType.Not)]
        [InlineData("and", TokenType.And)]
        [InlineData("or", TokenType.Or)]
        [InlineData("for", TokenType.For)]
        [InlineData("while", TokenType.While)]
        [InlineData("if", TokenType.If)]
        [InlineData("def", TokenType.Def)]
        public void ParseSingleTypeOnlyToken(string testString, TokenType expectedToken)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(expectedToken, token.Type);
        }
    
        [Theory]
        [InlineData("hello")]
        [InlineData("_internal_hello")]
        [InlineData("__private_hello")]
        [InlineData("__hello_2")]
        [InlineData("helloWorld")]
        [InlineData("HelloWorld")]
        [InlineData("trueHello")]
        [InlineData("falseHello")]
        public void ParseIdentifier(string identifier)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(identifier));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.Identifier, token.Type);
            Assert.Equal(identifier, token.Value);
        }
        
        [Theory]
        [InlineData("1hello")]
        [InlineData("_inter$#nal_hello")]
        [InlineData("hello#!@")]
        public void BadIdentifier(string identifier)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(identifier));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.Unknown, token.Type);
        }
    
        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void ParseLogicalConstant(string testString, bool expectedValue)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.LogicalConstant, token.Type);
            Assert.Equal(expectedValue, token.Value);
        }
    
        [Theory]
        [InlineData("0.5", 0.5)]
        [InlineData("0.25", 0.25)]
        [InlineData("2.75", 2.75)]
        [InlineData("24.54", 24.54)]
        public void ParseDecimalConstant(string testString, double expectedValue)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.DecimalConstant, token.Type);
            Assert.Equal(expectedValue, token.Value);
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("1234", 1234)]
        public void ParseIntegerConstant(string testString, int expectedValue)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.IntConstant, token.Type);
            Assert.Equal(expectedValue, token.Value);
        }

        [Theory]
        [InlineData("\"hello\"", "hello")]
        public void ParseStringLiteral(string testString, string expectedValue)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.StringLiteral, token.Type);
            Assert.Equal(expectedValue, token.Value);
        }

        [Theory]
        [InlineData("\"unfinished literal")]
        public void BadStringLiteral(string testString)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.Unknown, token.Type);
        }
    }
}