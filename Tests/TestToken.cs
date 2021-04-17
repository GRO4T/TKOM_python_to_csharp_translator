using System;
using PythonCSharpTranslator.Exception;
using Translator.Token;
using Xunit;

namespace Tests
{
    public class TestToken
    {
        [Fact]
        public void SetWrongValueType_ExceptionIsThrown()
        {
            var token = new Token();
            Assert.Throws<TokenWrongValueTypeException>(() => token.Value = false); 
        }
        
        [Fact]
        public void ValueTypeInt_Correct()
        {
            int testValue = 20;
            var token = new Token();
            token.Value = testValue;
            int tokenValue = (int) token.Value;
            Assert.True(tokenValue == testValue, $"token.Value is {tokenValue} should be {testValue}");
        }
        
        [Fact]
        public void ValueTypeDouble_Correct()
        {
            double testValue = 15.5;
            double tolerance = 0.01;
            var token = new Token();
            token.Value = testValue;
            double tokenValue = (double) token.Value;
            Assert.True(Math.Abs(tokenValue - testValue) < tolerance, $"token.Value is {tokenValue} should be {testValue}");
        }
        
        [Fact]
        public void ValueTypeString_Correct()
        {
            string testValue = "Correct string";
            var token = new Token();
            token.Value = testValue;
            string tokenValue = (string) token.Value;
            Assert.True(tokenValue == testValue, $"token.Value is {tokenValue} should be {testValue}");
        }
        
        
        
        
    }
}