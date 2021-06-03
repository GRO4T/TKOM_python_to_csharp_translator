using System;
using System.Collections.Generic;
using System.Transactions;
using Serilog;

using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public class SemanticAnalyzer
    {
        private Dictionary<string, Statement> _symbolTable = new();
        private Parser _parser; 
        
        public SemanticAnalyzer(Parser parser)
        {
            _parser = parser;
        }

        public bool IsEnd()
        {
            return _parser.IsEnd();
        }
        
        public Statement EvaluateNextStatement()
        {
            var statement = _parser.GetNextStatement();
            if (statement == null) return statement;
            // check bad statement
            if (statement.Type == BadStatementType)
            {
                Log.Error(statement.ToString());
                throw new TranslationError();
            }
            EvaluateStatementIfHasName(ref statement);
            EvaluateStatementIfDoesNotHaveName(statement);
            Log.Information($"Fetched statement:\n {statement}");
            return statement;
        }

        private void EvaluateStatementIfHasName(ref Statement statement)
        {
            string name;
            if ((name = statement.GetName()) != null)
            {
                // check symbol already declared
                if (_symbolTable.ContainsKey(name))
                {
                    if (statement.Type == VariableDefType || statement.Type == FunctionDefType)
                    {
                        throw new TranslationError($"Symbol {name} already declared", _parser.GetLineNumber());
                    }
                    EvaluateIfAssignmentAndSymbolDeclared(statement, _symbolTable[name]);
                }
                else
                {
                    EvaluateIfAssignmentAndNotDeclared(ref statement);
                    if (statement.Type == FunctionCallType)
                    {
                        throw new TranslationError($"Function {name} not declared", _parser.GetLineNumber());
                    }
                    _symbolTable.Add(name, statement);
                }
            }
        }

        private void EvaluateStatementIfDoesNotHaveName(Statement statement)
        {
            if (statement.GetName() == null)
            {
                
            }
        }

        private void EvaluateIfAssignmentAndSymbolDeclared(Statement statement, Statement declaredAs)
        {
            if (statement.Type == AssignmentStatementType)
            {
                if (declaredAs.Type == ConstantDefType)
                    throw new TranslationError($"Cannot modify constant", _parser.GetLineNumber());
                else if (declaredAs.Type == VariableDefType)
                {
                    var variableDef = (VariableDef) declaredAs;
                    var assignment = (AssignmentStatement) statement;
                    var varType = variableDef.VariableType;
                    var assignedType = EvaluateRValue(assignment.RightSide).ValueType;
                    if (variableDef.VariableType != assignedType)
                        throw new TranslationError($"Variable type {varType} and assigned type {assignedType} do not match");
                }
                else
                    throw new TranslationError(
                        $"Cannot modify symbol at {_parser.GetLineNumber()}. First declared as {declaredAs.Type}");
            }
        }

        private void EvaluateIfAssignmentAndNotDeclared(ref Statement statement)
        {
            if (statement.Type == AssignmentStatementType)
            {
                var assignmentStatement = (AssignmentStatement) statement;
                if (assignmentStatement.RightSide.IsConstantValue())
                {
                    statement = new ConstantDef
                    {
                        Name = assignmentStatement.LeftSide,
                        RightSide = assignmentStatement.RightSide,
                        ConstantType = EvaluateConstantType(assignmentStatement.RightSide.GetValue().Type)
                        
                    };
                }
                else
                {
                    throw new TranslationError($"Variable {assignmentStatement.LeftSide} not declared", _parser.GetLineNumber());
                }
            }
        }
        
        private RValue EvaluateRValue(RValue rValue)
        {
            switch (rValue.Type)
            {
                case RValue.RValueType.Value:
                    if (rValue.IsConstantValue())
                        rValue.ValueType = EvaluateConstantType(rValue.GetValue().Type);
                    else if (rValue.GetValue().Type == TokenType.Identifier)
                    {
                        var identifierName = rValue.GetValue().Value.GetString();
                        Log.Debug(identifierName);
                        if (!_symbolTable.ContainsKey(identifierName))
                            throw new TranslationError($"Symbol {identifierName} not declared",
                                _parser.GetLineNumber());
                        else
                        {
                            var symbol = _symbolTable[identifierName];
                            if (symbol.Type != VariableDefType)
                                throw new TranslationError($"Symbol declared as {symbol.Type} used as VariableDefType");
                            var variableDef = (VariableDef) symbol;
                            rValue.ValueType = variableDef.VariableType;
                        }
                    }
                    break;
                case RValue.RValueType.FunCall:
                    var funCall = rValue.GetFunCall();
                    EvaluateFunctionCall(funCall);
                    var returnType = ((FunctionDef) _symbolTable[funCall.Name]).ReturnType;
                    if (returnType != null)
                        rValue.ValueType = (TokenType) returnType;
                    break;
                case RValue.RValueType.ArithmeticExpression:
                    rValue.ValueType = EvaluateArithmeticExpression(rValue.GetArithmeticExpression());
                    break;
                case RValue.RValueType.LogicalExpression:
                    EvaluateLogicalExpression(rValue.GetLogicalExpression());
                    rValue.ValueType = TokenType.BoolToken;
                    break;
            }
            
            return rValue;
        }

        private void EvaluateLogicalExpression(List<Token> expression)
        {
            var tokenIterator = expression.GetEnumerator();
            while (tokenIterator.MoveNext())
            {
                var token = tokenIterator.Current;
                if (token.IsParameter())
                {
                    EvaluateRValue(new RValue(token));
                }
                else if (token.Type == TokenType.NotToken)
                {
                    if (!tokenIterator.MoveNext())
                        throw new TranslationError("Found not token but nothing after it");
                    var rvalue = EvaluateRValue(new RValue(tokenIterator.Current));
                    if (rvalue.ValueType != TokenType.BoolToken)
                        throw new TranslationError("Cannot negate non-boolean value");
                }
            }
        }

        private TokenType EvaluateArithmeticExpression(List<Token> expression)
        {
            TokenType expressionType = TokenType.UnknownToken;
            foreach (var token in expression)
            {
                if (token.IsParameter())
                {
                    var rvalue = EvaluateRValue(new RValue(token));
                    if (expressionType == TokenType.UnknownToken)
                        expressionType = rvalue.ValueType;
                    else if (rvalue.ValueType != expressionType)
                        throw new TranslationError("Cannot determine arithmetic expression type",
                            _parser.GetLineNumber());
                }
            }
            return expressionType;
        }

        private void EvaluateFunctionCall(FunctionCall functionCall)
        {
            if (!_symbolTable.ContainsKey(functionCall.Name))
                throw new TranslationError($"Function {functionCall.Name} not declared", _parser.GetLineNumber());
            else
            {
                var symbol = _symbolTable[functionCall.Name];
                if (symbol.Type != FunctionDefType)
                    throw new TranslationError($"Symbol declared as {symbol.Type} but used as {functionCall.Type}", _parser.GetLineNumber());
                var functionDef = (FunctionDef) symbol;
                if (functionCall.Args.Count != functionDef.ArgList.Count)
                    throw new TranslationError($"Number of arguments does not match", _parser.GetLineNumber());
                for (int i = 0; i < functionCall.Args.Count; i++)
                {
                    var gotToken = functionCall.Args[i];
                    var expectedType = functionDef.ArgList[i].Item2;
                    EvaluateRValue(new RValue(gotToken));
                    var gotTokenType = gotToken.IsConstantValue() ? EvaluateConstantType(gotToken.Type) : gotToken.Type;
                    if (gotTokenType != expectedType)
                        throw new TranslationError(
                            $"Argument number {i + 1}. Types do not match. Expected {expectedType}. Got {gotTokenType}", _parser.GetLineNumber());
                }
            }
        }

        private void EvaluateFunctionDef(FunctionDef functionDef)
        {
            if (!_symbolTable.ContainsKey(functionDef.Name))
                throw new TranslationError($"Symbol {functionDef.Name} already declared");
            
        }
        
        private static TokenType EvaluateConstantType(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.IntegerConstant:
                    return TokenType.IntToken;
                case TokenType.DecimalConstant:
                    return TokenType.FloatToken;
                case TokenType.StringLiteral:
                    return TokenType.StrToken;
                case TokenType.LogicalConstant:
                    return TokenType.BoolToken;
                default:
                    throw new TranslationError($"Unrecognised constant type {tokenType}");
            }
        }
    }
}