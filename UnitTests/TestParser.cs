using System.Collections.Generic;
using System.Diagnostics;
using PythonCSharpTranslator;
using Serilog;
using Xunit;
using static PythonCSharpTranslator.StatementType;
using static PythonCSharpTranslator.TokenType;

namespace Tests
{
    public class TestParser
    {
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
            Assert.Equal("hello", assignStatement.LeftSide);
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
        [InlineData("Resources/parser/simple_logical_expression.py", AssignmentStatementType)]
        [InlineData("Resources/parser/double_brackets.py", AssignmentStatementType)]
        [InlineData("Resources/parser/logical_expression_with_not.py", AssignmentStatementType)]
        [InlineData("Resources/parser/logical_expression_with_not_and_and.py", AssignmentStatementType)]
        [InlineData("Resources/parser/not_without_brackets.py", AssignmentStatementType)]
        [InlineData("Resources/parser/brackets_not_match.py", BadStatementType)]
        [InlineData("Resources/parser/double_not.py", BadStatementType)]
        [InlineData("Resources/parser/empty_brackets.py", BadStatementType)]
        [InlineData("Resources/parser/empty_logical_expression.py", BadStatementType)]
        [InlineData("Resources/parser/no_right_operand.py", BadStatementType)]
        [InlineData("Resources/parser/arithmetic_expression1.py", AssignmentStatementType)]
        [InlineData("Resources/parser/arithmetic_expression2.py", AssignmentStatementType)]
        [InlineData("Resources/parser/if_statement.py", IfStatementType)]
        [InlineData("Resources/parser/while_statement.py", WhileLoopType)]
        [InlineData("Resources/parser/for_loop.py", ForLoopType)]
        [InlineData("Resources/parser/function_def_no_args_no_ret_value.py", FunctionDefType)]
        [InlineData("Resources/parser/function_def_no_args_ret_value.py", FunctionDefType)]
        [InlineData("Resources/parser/function_def_one_arg.py", FunctionDefType)]
        [InlineData("Resources/parser/function_def_mult_args_ret_value.py", FunctionDefType)]
        public void ParseSingleStatement(string filename, StatementType expectedStatement)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            var s = parser.GetNextStatement();
            Assert.Equal(expectedStatement, s.Type);
        }

        [Theory]
        [InlineData("Resources/parser/function_def_then_var_def.py", new [] {FunctionDefType, VariableDefType})]
        [InlineData("Resources/parser/double_assignment.py", new [] {AssignmentStatementType, AssignmentStatementType})]
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
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/function_call.py")));
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
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/variable_def.py")));
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
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/function_def_mult_args_ret_value.py")));
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
            Assert.Equal(ReturnStatementType, funDef.Statements[1].Type); 
        }

        [Fact]
        public void ForLoopBuiltCorrectly()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/for_loop.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(ForLoopType, s.Type);
            var forLoop = (ForLoop) s;
            Assert.Equal("i", forLoop.IteratorName);
            Assert.Equal(0, forLoop.Start);
            Assert.Equal(5, forLoop.End);
            Assert.Equal(AssignmentStatementType, forLoop.Statements[0].Type); 
        }

        [Fact]
        public void AssignmentBuiltCorrectly()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/arithmetic_expression1.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(AssignmentStatementType, s.Type);
            var assignStatement = (AssignmentStatement) s;
            Assert.Equal("x", assignStatement.LeftSide);
            Assert.Equal(RValue.RValueType.ArithmeticExpression, assignStatement.RightSide.Type);
            var tokenList = assignStatement.RightSide.GetArithmeticExpression();
            Assert.Equal(7, tokenList.Count);
            Assert.Equal(2, tokenList[0].Value.GetInt());
            Assert.Equal(Plus, tokenList[1].Type);
            Assert.Equal(LeftParenthesis, tokenList[2].Type);
            Assert.Equal(3, tokenList[3].Value.GetInt());
            Assert.Equal(Star, tokenList[4].Type);
            Assert.Equal(2, tokenList[5].Value.GetInt());
            Assert.Equal(RightParenthesis, tokenList[6].Type);
        }
        
        [Fact]
        public void IfStatementBuiltCorrectly()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/if_statement.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(IfStatementType, s.Type);
            var ifStatement = (IfStatement) s;
            Assert.Equal(7, ifStatement.Condition.Count);
            Assert.Equal(LeftParenthesis, ifStatement.Condition[0].Type);
            Assert.Equal(RightParenthesis, ifStatement.Condition[6].Type);
            Assert.Equal(AssignmentStatementType, ifStatement.Statements[0].Type);
        }

        [Fact]
        public void WhileLoopBuiltCorrectly()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/while_statement.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(WhileLoopType, s.Type);
            var whileLoop = (WhileLoop) s;
            Assert.Equal(5, whileLoop.Condition.Count);
            Assert.Equal(Identifier, whileLoop.Condition[1].Type);
            Assert.Equal(IntegerConstant, whileLoop.Condition[3].Type);
        }

        [Fact]
        public void NestedStatements()
        {
            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/parser/nested_statements.py")));
            var s = parser.GetNextStatement();
            Assert.Equal(FunctionDefType, s.Type);
            var functionDef = (FunctionDef) s;
            Assert.Equal(IfStatementType, functionDef.Statements[0].Type);
            var ifStatement = (IfStatement)functionDef.Statements[0];
            Assert.Equal(AssignmentStatementType, ifStatement.Statements[0].Type);
            Assert.Equal(ReturnStatementType, ifStatement.Statements[1].Type);
        }

        [Theory]
        [InlineData("Resources/parser/equal_not_brackets.py", new [] {Identifier, EqualSymbol, IntegerConstant})]
        public void ParseLogicalExpression(string filename, TokenType[] expectedTokens)
        {
            var parser = new Parser(new Lexer(new FileCharacterSource(filename)));
            var s = parser.GetNextStatement();
            Assert.Equal(AssignmentStatementType, s.Type);
            var assignment = (AssignmentStatement) s;
            Assert.Equal(RValue.RValueType.LogicalExpression, assignment.RightSide.Type);
            var expression = assignment.RightSide.GetLogicalExpression();
            Assert.Equal(expectedTokens.Length, expression.Count);
            for (int i = 0; i < expectedTokens.Length; i++) 
            {
                Assert.Equal(expectedTokens[i], expression[i].Type);
            }
        }
        
    }
}