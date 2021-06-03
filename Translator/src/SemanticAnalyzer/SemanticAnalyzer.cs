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
        
        public Statement AnalyzeNextStatement()
        {
            var s = _parser.GetNextStatement();
            if (s == null) return s;
            // check bad statement
            if (s.Type == BadStatementType)
            {
                Log.Error(s.ToString());
                throw new TranslationError();
            }
            string name;
            if ((name = s.GetName()) != null)
            {
                // check symbol already declared
                if (_symbolTable.ContainsKey(name))
                {
                    if (s.Type == VariableDefType || s.Type == FunctionDefType)
                    {
                        throw new TranslationError($"Symbol {name} already declared", _parser.GetLineNumber());
                    }
                    else if (s.Type == AssignmentStatementType)
                    {
                        var symbol = _symbolTable[name];
                        if (symbol.Type == ConstantDefType)
                            throw new TranslationError($"Cannot modify constant", _parser.GetLineNumber());
                        else if (symbol.Type == VariableDefType)
                        {
                            var variableDef = (VariableDef) symbol;
                            var assignment = (AssignmentStatement) s;
                            var varType = variableDef.VariableType;
                            var assignedType = EvaluateRValue(assignment.RightSide).ValueType;
                            if (variableDef.VariableType != assignedType)
                                throw new TranslationError($"Variable type {varType} and assigned type {assignedType} do not match");
                        }
                        else
                            throw new TranslationError(
                                $"Cannot modify symbol at {_parser.GetLineNumber()}. First declared as {symbol.Type}");
                    }
                }
                else
                {
                    if (s.Type == AssignmentStatementType)
                    {
                        var assignmentStatement = (AssignmentStatement) s;
                        if (assignmentStatement.RightSide.IsConstantValue())
                        {
                            s = new ConstantDef
                            {
                                Name = assignmentStatement.LeftSide,
                                RightSide = assignmentStatement.RightSide,
                                ConstantType = EvaluateConstantType(assignmentStatement.RightSide.GetValue().Type)
                                
                            };
                        }
                        else
                        {
                            throw new TranslationError($"Variable {name} not declared", _parser.GetLineNumber());
                        }
                    }
                    else if (s.Type == FunctionCallType)
                    {
                        throw new TranslationError($"Function {name} not declared", _parser.GetLineNumber());
                    }
                    _symbolTable.Add(name, s);
                }
            }
            // if (s.Type == Fun)
            Log.Information($"Fetched statement:\n {s}");
            return s;
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
            }
            return rValue;
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