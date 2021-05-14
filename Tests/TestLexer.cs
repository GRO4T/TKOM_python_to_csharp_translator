﻿using System.Collections.Generic;
using PythonCSharpTranslator;
using Xunit;
using static PythonCSharpTranslator.TokenType;

namespace Tests
{
    public class TestLexer
    {
        [Theory]
        [InlineData("", End)]
        [InlineData("int", IntegerType)]
        [InlineData("str", StringType)]
        [InlineData("float", DecimalType)]
        [InlineData("bool", BooleanType)]
        [InlineData("(", LeftParenthesis)]
        [InlineData(")", RightParenthesis)]
        [InlineData(",", Comma)]
        [InlineData(":", Colon)]
        [InlineData("=", AssignmentSymbol)]
        [InlineData("+", Plus)]
        [InlineData("-", Minus)]
        [InlineData("*", Star)]
        [InlineData("/", Slash)]
        [InlineData("return", Return)]
        [InlineData("<", LessThan)]
        [InlineData(">", GreaterThan)]
        [InlineData("==", EqualSymbol)]
        [InlineData("!=", NotEqualSymbol)]
        [InlineData("<=", LessEqualThan)]
        [InlineData(">=", GreaterEqualThan)]
        [InlineData("not", Not)]
        [InlineData("and", And)]
        [InlineData("or", Or)]
        [InlineData("for", For)]
        [InlineData("while", While)]
        [InlineData("if", If)]
        [InlineData("def", Def)]
        [InlineData("\n", Newline)]
        [InlineData("\t", Indent)]
        [InlineData("->", Arrow)]
        [InlineData("a", Identifier)]
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
            Assert.Equal(Identifier, token.Type);
            Assert.Equal(identifier, token.Value.GetString());
        }
        
        [Theory]
        [InlineData("1hello")]
        [InlineData("_inter$#nal_hello")]
        [InlineData("hello#!@")]
        public void BadIdentifier(string identifier)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(identifier));
            Token token = lexer.GetNextToken();
            Assert.Equal(Unknown, token.Type);
        }
    
        [Theory]
        [InlineData("True", true)]
        [InlineData("False", false)]
        public void ParseLogicalConstant(string testString, bool expectedValue)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(LogicalConstant, token.Type);
            Assert.Equal(expectedValue, token.Value.GetBool());
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
            Assert.Equal(DecimalConstant, token.Type);
            Assert.Equal(expectedValue, token.Value.GetDouble());
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("1234", 1234)]
        public void ParseIntegerConstant(string testString, int expectedValue)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(IntegerConstant, token.Type);
            Assert.Equal(expectedValue, token.Value.GetInt());
        }

        [Theory]
        [InlineData("\"hello\"", "hello")]
        public void ParseStringLiteral(string testString, string expectedValue)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(StringLiteral, token.Type);
            Assert.Equal(expectedValue, token.Value.GetString());
        }

        [Theory]
        [InlineData("\"unfinished literal")]
        public void BadStringLiteral(string testString)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(Unknown, token.Type);
        }
    
        [Theory]
        [InlineData("test_integer = int(3)\n", 
            new[]{Identifier, AssignmentSymbol, IntegerType, LeftParenthesis, IntegerConstant, RightParenthesis, Newline})]
        [InlineData("def hello(arg: int) -> float:\n\tx = 1", 
            new[]
            {
                Def, Identifier, LeftParenthesis, Identifier, Colon, IntegerType, RightParenthesis, Arrow, DecimalType, Colon,
                Newline, Indent, Identifier, AssignmentSymbol, IntegerConstant 
            })]
        [InlineData("intValue = 1\n", new[]{Identifier, AssignmentSymbol, IntegerConstant, Newline})]
        public void ParseStatement(string testString, TokenType[] expectedTokens)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token;
            var tokens = new List<TokenType>();
            while ((token = lexer.GetNextToken()).Type != End)
            {
                tokens.Add(token.Type); 
            }
            Assert.Equal(tokens.Count, expectedTokens.Length);
            for (int i = 0; i < expectedTokens.Length; i++)
            {
                Assert.Equal(tokens[i], expectedTokens[i]);
            }
        }
    
        [Theory]
        [InlineData("Resources/int_value_then_float_value.py", new[]
        {
            Identifier, AssignmentSymbol, IntegerConstant, Newline,
            Identifier, AssignmentSymbol, DecimalConstant
        })]
        [InlineData("Resources/function.py", new[]
        {
            Def, Identifier, LeftParenthesis, Identifier, Colon, IntegerType, RightParenthesis, Arrow,
            DecimalType, Colon, Newline, Indent, Identifier, AssignmentSymbol, IntegerConstant 
        })]
        public void ParseBlock(string filename, TokenType[] expectedTokens)
        {
            Lexer lexer = new Lexer(new FileCharacterSource(filename));
            Token token;
            var tokens = new List<TokenType>();
            while ((token = lexer.GetNextToken()).Type != End)
            {
                tokens.Add(token.Type); 
            }
            Assert.Equal(tokens.Count, expectedTokens.Length);
            for (int i = 0; i < expectedTokens.Length; i++)
            {
                Assert.Equal(expectedTokens[i], tokens[i]);
            }
        }
    
        [Fact]
        public void ParseFunctionCheckIdentifierValues()
        {
            Lexer lexer = new Lexer(new FileCharacterSource("Resources/function.py"));
            Token token;
            var tokens = new List<Token>();
            while ((token = lexer.GetNextToken()).Type != End)
            {
                tokens.Add(token); 
            }

            List<Token> identifiers = tokens.FindAll((t) => t.Type == Identifier);
            Assert.Equal("hello", identifiers[0].Value.GetString());
            Assert.Equal("arg", identifiers[1].Value.GetString());
            Assert.Equal("x", identifiers[2].Value.GetString());
        }

        [Theory]
        [InlineData("# var = 1", new TokenType[0])]
        [InlineData("# var = 1\nvar2 = 2", new[] { Identifier, AssignmentSymbol, IntegerConstant })]
        [InlineData("var = 1 # some comment\n", new[] { Identifier, AssignmentSymbol, IntegerConstant })]
        public void IgnoreComments(string testString, TokenType[] expectedTokens)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token;
            var tokens = new List<TokenType>();
            while ((token = lexer.GetNextToken()).Type != End)
            {
                tokens.Add(token.Type); 
            }
            Assert.Equal(tokens.Count, expectedTokens.Length);
            for (int i = 0; i < expectedTokens.Length; i++)
            {
                Assert.Equal(tokens[i], expectedTokens[i]);
            }
        }
    }
}