using System.Collections.Generic;
using Serilog;
using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public class SemanticAnalyzer
    {
        // TODO move this to context
        private Dictionary<string, Statement> _symbolTable = new();
        private TokenType? _returnType = null;
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
            return EvaluateStatement(_parser.GetNextStatement());
        }

        private Statement EvaluateStatement(Statement statement, TokenType? returnType)
        {
            if (statement == null) return statement;
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
                        throw new TranslationError($"Symbol {name} already declared", statement.LineNumber);
                    }
                    EvaluateIfAssignmentAndSymbolDeclared(statement, _symbolTable[name]);
                }
                else
                {
                    EvaluateIfAssignmentAndNotDeclared(ref statement);
                    if (statement.Type == FunctionCallType)
                    {
                        throw new TranslationError($"Function {name} not declared", statement.LineNumber);
                    }
                    if (statement.Type == FunctionDefType)
                        EvaluateFunctionDef((FunctionDef) statement);
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
                    throw new TranslationError($"Cannot modify constant", statement.LineNumber);
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
                        $"Cannot modify symbol at {statement.LineNumber}. First declared as {declaredAs.Type}");
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
                        ConstantType = EvaluateConstantType(assignmentStatement.RightSide.GetValue().Type),
                        LineNumber = assignmentStatement.LineNumber
                    };
                }
                else
                {
                    throw new TranslationError($"Variable {assignmentStatement.LeftSide} not declared", statement.LineNumber);
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
                            throw new TranslationError($"Symbol {identifierName} not declared");
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
                        throw new TranslationError("Found not token but nothing after it", token.LineNumber);
                    var rvalue = EvaluateRValue(new RValue(tokenIterator.Current));
                    if (rvalue.ValueType != TokenType.BoolToken)
                        throw new TranslationError("Cannot negate non-boolean value", token.LineNumber);
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
                        throw new TranslationError("Cannot determine arithmetic expression type", expression[0].LineNumber);
                }
            }
            return expressionType;
        }

        private void EvaluateFunctionCall(FunctionCall functionCall)
        {
            if (!_symbolTable.ContainsKey(functionCall.Name))
                throw new TranslationError($"Function {functionCall.Name} not declared", functionCall.LineNumber);
            else
            {
                var symbol = _symbolTable[functionCall.Name];
                if (symbol.Type != FunctionDefType)
                    throw new TranslationError($"Symbol declared as {symbol.Type} but used as {functionCall.Type}", functionCall.LineNumber);
                var functionDef = (FunctionDef) symbol;
                if (functionCall.Args.Count != functionDef.ArgList.Count)
                    throw new TranslationError($"Number of arguments does not match", functionCall.LineNumber);
                for (int i = 0; i < functionCall.Args.Count; i++)
                {
                    var gotToken = functionCall.Args[i];
                    var expectedType = functionDef.ArgList[i].Item2;
                    EvaluateRValue(new RValue(gotToken));
                    TokenType gotTokenType = TokenType.UnknownToken;
                    if (gotToken.IsConstantValue())
                        gotTokenType = EvaluateConstantType(gotToken.Type);
                    else
                        gotTokenType = ((VariableDef)_symbolTable[gotToken.Value.GetString()]).VariableType;
                    if (gotTokenType != expectedType)
                        throw new TranslationError(
                            $"Argument number {i + 1}. Types do not match. Expected {expectedType}. Got {gotTokenType}",
                            functionCall.LineNumber);
                }
            }
        }

        private void EvaluateFunctionDef(FunctionDef functionDef)
        {
            var symbolTableCopy = new Dictionary<string, Statement>(_symbolTable);
            foreach (var arg in functionDef.ArgList)
            {
                _symbolTable[arg.Item1] = new VariableDef {VariableType = arg.Item2};
            }
            EvaluateBlock(functionDef.Statements, functionDef.ReturnType);
            _symbolTable = new Dictionary<string, Statement>(symbolTableCopy);
        }

        private void EvaluateBlock(List<Statement> statements, TokenType? returnType)
        {
            foreach (var statement in statements)
            {
                EvaluateStatement(statement, returnType);
                if (statement.Type == ReturnStatementType)
                {
                    var returnStatement = (ReturnStatement) statement;
                    var foundType = EvaluateRValue(new RValue(returnStatement.Value)).ValueType;
                    if (returnType == null)
                        throw new TranslationError($"Function should not return a value", statement.LineNumber);
                    if (foundType != returnType)
                        throw new TranslationError(
                            $"Wrong return type. Got {foundType}. Expected {returnType}", statement.LineNumber);
                }
                    
            }
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