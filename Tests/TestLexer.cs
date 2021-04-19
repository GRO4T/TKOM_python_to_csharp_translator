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
        public void ParseCorrectIdentifier(string identifier)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(identifier));
            Token token = lexer.GetNextToken();
            Assert.Equal(TokenType.Identifier, token.Type);
            Assert.Equal(identifier, token.Value);
        }
    }
}