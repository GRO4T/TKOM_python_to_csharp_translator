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
        public void ParseSingleTypeOnlyToken(string testString, TokenType expectedToken)
        {
            Lexer lexer = new Lexer(new StringCharacterSource(testString));
            Token token = lexer.GetNextToken();
            Assert.Equal(expectedToken, token.Type);
        }
    }
}