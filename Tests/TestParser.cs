﻿using System.Collections.Generic;
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
            Assert.Equal("hello", variableDef.Name);
            Assert.Equal("world", variableDef.InitialValue.Value.GetString());
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
            Assert.Equal(FunctionCallType, s.Type);
            var funCall = (FunctionCall) s;
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
        [InlineData("Resources/function_def_no_args_no_ret_value.py", FunctionDefType)]
        [InlineData("Resources/function_def_no_args_ret_value.py", FunctionDefType)]
        [InlineData("Resources/function_def_one_arg.py", FunctionDefType)]
        [InlineData("Resources/function_def_mult_args_ret_value.py", FunctionDefType)]
        public void ParseSingleStatement(string filename, StatementType expectedStatement)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            var s = parser.GetNextStatement();
            Assert.Equal(expectedStatement, s.Type);
        }

        [Theory]
        [InlineData("Resources/function_def_then_var_def.py", new [] {FunctionDefType, VariableDefType})]
        public void ParseMultipleStatements(string filename, StatementType[] expectedStatements)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            foreach (var expectedStatement in expectedStatements)
            {
                var s = parser.GetNextStatement();
                Assert.Equal(expectedStatement, s.Type);
            }
        }

        [Fact]
        public void FunctionCallObjectBuiltCorrectly()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/function_call.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(FunctionCallType, s.Type);
            var funCall = (FunctionCall) s;
            Assert.Equal("func_call", funCall.Name);
            Assert.Equal(Identifier, funCall.Args[0].Type);
            Assert.Equal("arg", funCall.Args[0].Value.GetString());
            Assert.Equal(StringLiteral, funCall.Args[1].Type);
            Assert.Equal("hello", funCall.Args[1].Value.GetString());
        }

        [Fact]
        public void VariableDefBuiltCorrectly()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/variable_def.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(VariableDefType, s.Type);
            var varDef = (VariableDef) s;
            Assert.Equal("var_def", varDef.Name);
            Assert.Equal(BoolToken, varDef.VariableType);
            Assert.Equal(LogicalConstant, varDef.InitialValue.Type);
            Assert.True(varDef.InitialValue.Value.GetBool());
        }

        [Fact]
        public void FunctionDefBuiltCorrectly()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/function_def_mult_args_ret_value.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(FunctionDefType, s.Type);
            var funDef = (FunctionDef) s;
            Assert.Equal("hello", funDef.Name);
            Assert.Equal(BoolToken, funDef.ReturnType);
            // check args
            Assert.Equal("x", funDef.ArgList[0].Item1);
            Assert.Equal(IntToken, funDef.ArgList[0].Item2);
            Assert.Equal("y", funDef.ArgList[1].Item1);
            Assert.Equal(FloatToken, funDef.ArgList[1].Item2);
            Assert.Equal("z", funDef.ArgList[2].Item1);
            Assert.Equal(StrToken, funDef.ArgList[2].Item2);
            // check statements
            Assert.Equal(VariableDefType, funDef.Statements[0].Type); 
            Assert.Equal(ReturnStatement, funDef.Statements[1].Type); 
        }
    }
}