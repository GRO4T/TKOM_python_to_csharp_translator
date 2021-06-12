using System;
using System.Collections;
using System.Collections.Generic;
using Serilog;
using static PythonCSharpTranslator.TokenType;
using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public class Parser
    {
        private readonly ITokenSource _tokenSource;
        private Token _currentToken;
        private List<Token> _tokens;
        public bool SourceEnd;
        
        public Parser(ITokenSource tokenSource)
        {
            _tokenSource = tokenSource;
        }

        public bool IsEnd()
        {
            return SourceEnd;
        }

        public Statement? GetNextStatement(int nestingLevel = 0)
        {
            _tokens = new List<Token>();
            while (_currentToken == null || _currentToken.Type == NewlineToken)
                GetToken(false);
            _tokens.Add(_currentToken);
            int lineNumber = _currentToken.LineNumber;
            Statement? statement;
            if ((statement = ParseFuncCallOrVarDefOrAssign()) != null)
                return AddLineNumberAndReturn(statement, lineNumber);
            if ((statement = ParseIfStatement(nestingLevel)) != null)
                return AddLineNumberAndReturn(statement, lineNumber);
            if ((statement = ParseWhileLoop(nestingLevel)) != null)
                return AddLineNumberAndReturn(statement, lineNumber);
            if ((statement = ParseForLoop(nestingLevel)) != null)
                return AddLineNumberAndReturn(statement, lineNumber);
            if ((statement = ParseFunctionDef(nestingLevel)) != null)
                return AddLineNumberAndReturn(statement, lineNumber);
            if ((statement = ParseReturnStatement()) != null)
                return AddLineNumberAndReturn(statement, lineNumber);
            if (IsEnd())
                return null;
            return CreateBadStatement("Cannot recognize statement");
        }

        private Statement AddLineNumberAndReturn(Statement statement, int lineNumber)
        {
            statement.LineNumber = lineNumber;
            return statement;
        }

        private Statement ParseReturnStatement()
        {
            if (_currentToken.Type == Return)
            {
                GetToken();
                if (_currentToken.IsParameter())
                {
                    GetToken();
                    return new ReturnStatement {Value = _tokens[^2]};
                }
                return CreateBadStatement("Expected either value or an identifier");
            }
            return null;
        }

        private Statement? ParseFuncCallOrVarDefOrAssign()
        {
            if (_currentToken.Type == Identifier)
            {
                 GetToken();
                 if (_currentToken.Type == AssignmentSymbol)
                     return ParseVarDefOrAssign();
                 if (_currentToken.Type == LeftParenthesis)
                     return ParseFunCall();
            }
            return null;
        }

        private Statement? ParseFunctionDef(int nestingLevel)
        {
            var parseClosure = new Func<List<Tuple<string, TokenType>>, Statement>((argList) =>
            {
                var functionDef = new FunctionDef {Name = _tokens[1].Value.GetString(), ArgList = argList};
                GetToken();
                if (_currentToken.Type == Colon)
                {
                    GetToken(); 
                    Statement outStatement = null;
                    if (!ParseBlock(ref functionDef.Statements, nestingLevel, ref outStatement))
                    {
                        Log.Error(outStatement.ToString());
                        return CreateBadStatement("Error parsing block");
                    }
                    return functionDef;
                }
                if (_currentToken.Type == Arrow)
                {
                    GetToken();
                    if (!_currentToken.IsType()) return CreateBadStatement("Expected type specifier");
                    functionDef.ReturnType = _currentToken.Type;
                    GetToken();
                    if (_currentToken.Type != Colon) return CreateBadStatement("Statement should end with a colon");
                    GetToken();
                    Statement outStatement = null;
                    if (!ParseBlock(ref functionDef.Statements, nestingLevel, ref outStatement))
                    {
                        Log.Error(outStatement.ToString());
                        return CreateBadStatement("Error parsing block");
                    }
                    return functionDef;
                }
                return CreateBadStatement("Function declaration should end with either a colon or return type specifier");
            });
            // logic
            if (_currentToken.Type == DefToken)
            {
                var argList = new List<Tuple<string, TokenType>>();
                GetToken();
                if (_currentToken.Type != Identifier) return CreateBadStatement("Expected function name");
                GetToken();
                if (_currentToken.Type != LeftParenthesis) return CreateBadStatement("Expected opening bracket");
                GetToken();
                if (_currentToken.Type == RightParenthesis) return parseClosure(argList);
                while (true)
                {
                    if (_currentToken.Type != Identifier) return CreateBadStatement("Expected argument name");
                    var argName = _currentToken.Value.GetString();
                    GetToken();                    
                    if (_currentToken.Type != Colon) return CreateBadStatement("Expected colon after argument name");
                    GetToken();
                    if (!_currentToken.IsType()) return CreateBadStatement("Expected type specifier");
                    argList.Add(new Tuple<string, TokenType>(argName, _currentToken.Type)); 
                    GetToken();
                    if (_currentToken.Type == RightParenthesis) return parseClosure(argList);
                    if (_currentToken.Type != Comma) return CreateBadStatement("Arguments should be separated by a comma");
                    GetToken();
                }
            }
            return null;
        }

        private Statement? ParseForLoop(int nestingLevel)
        {
            if (_currentToken.Type == ForToken)
            {
                var forLoop = new ForLoop();
                GetToken();
                if (_currentToken.Type != Identifier) return CreateBadStatement("Iterator not specified");
                forLoop.IteratorName = _currentToken.Value.GetString();
                GetToken();
                if (_currentToken.Type != InToken) return CreateBadStatement("Expected 'in' keyword");
                GetToken();
                if (_currentToken.Type != RangeToken) return CreateBadStatement("Expected 'range' keyword");
                GetToken();
                if (_currentToken.Type != LeftParenthesis) return CreateBadStatement("Expected an opening bracket");
                GetToken();
                if (_currentToken.Type != IntegerConstant) return CreateBadStatement("Expected an integer");
                forLoop.Start = _currentToken.Value.GetInt();
                GetToken();
                if (_currentToken.Type != Comma) return CreateBadStatement("Start and end should be separated by a comma");
                GetToken();
                if (_currentToken.Type != IntegerConstant) return CreateBadStatement("Expected an integer");
                forLoop.End = _currentToken.Value.GetInt();
                GetToken();
                if (_currentToken.Type != RightParenthesis) return CreateBadStatement("Expected a closing bracket");
                GetToken();
                if (_currentToken.Type != Colon) return CreateBadStatement("Statement should end with a colon");
                GetToken();
                Statement outStatement = null;
                if (!ParseBlock(ref forLoop.Statements, nestingLevel, ref outStatement))
                {
                    Log.Error(outStatement.ToString());
                    return CreateBadStatement("Error parsing block");
                }
                return forLoop;
            }
            return null;
        }

        private Statement? ParseWhileLoop(int nestingLevel)
        {
            Statement outStatement = null;
            if (_currentToken.Type == WhileToken)
            {
                var whileLoop = new WhileLoop();
                RValue.RValueType type = RValue.RValueType.Undefined;
                GetToken();
                if (!ParseBracketExpression(ref type, ref outStatement))
                {
                    if (type == RValue.RValueType.ArithmeticExpression)
                        return CreateBadStatement("Condition not a logical expression");
                    Log.Error(outStatement.ToString());
                    return CreateBadStatement("Error parsing bracket expression");
                }
                whileLoop.Condition = _tokens.GetRange(1, _tokens.Count - 2);
                if (_currentToken.Type != Colon) return CreateBadStatement("Statement should end with a colon");
                GetToken();
                if (!ParseBlock(ref whileLoop.Statements, nestingLevel, ref outStatement))
                {
                    Log.Error(outStatement.ToString());
                    return CreateBadStatement("Error parsing block");
                }
                return whileLoop;
            }
            return null;
        }

        private Statement? ParseIfStatement(int nestingLevel)
        {
            Statement outStatement = null;
            if (_currentToken.Type == IfToken)
            {
                var ifStatement = new IfStatement();
                RValue.RValueType type = RValue.RValueType.Undefined;
                GetToken();
                if (!ParseBracketExpression(ref type, ref outStatement))
                {
                    if (type == RValue.RValueType.ArithmeticExpression)
                        return CreateBadStatement("Condition not a logical expression");
                    Log.Error(outStatement.ToString());
                    return CreateBadStatement("Error parsing bracket expression");
                }
                ifStatement.Condition = _tokens.GetRange(1, _tokens.Count - 2); 
                if (_currentToken.Type != Colon) return CreateBadStatement("Statement should end with a colon");
                GetToken();
                if (!ParseBlock(ref ifStatement.Statements, nestingLevel, ref outStatement))
                {
                    Log.Error(outStatement.ToString());
                    return CreateBadStatement("Error parsing block");
                }
                return ifStatement;
            }
            return null;
        }

        private Statement ParseFunCall()
        {
            var funCall = new FunctionCall{Name=_tokens[^2].Value.GetString(), LineNumber = _tokens[^2].LineNumber};
            GetToken();
            if (_currentToken.Type == RightParenthesis)
            {
                GetToken();
                return funCall; // no arguments
            }
            if (!_currentToken.IsParameter()) return CreateBadStatement("Argument should be either a value or an identifier");
            funCall.Args.Add(_currentToken);
            GetToken();
            if (_currentToken.Type == RightParenthesis) // single argument
            {
                GetToken();
                return funCall;
            }
            while (_currentToken.Type != RightParenthesis) // multiple arguments
            {
                if (_currentToken.Type != Comma) return CreateBadStatement("Expected comma after each consequential argument");
                GetToken();
                if (!_currentToken.IsParameter()) return CreateBadStatement("Argument should be either a value or an identifier");
                funCall.Args.Add(_currentToken);
                GetToken();
            }
            GetToken();
            return funCall;
        }


        private Statement ParseVarDefOrAssign()
        {
            GetToken();
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier, LeftParenthesis, NotToken}).Contains(
                _currentToken.Type))
                return ParseAssignment();
            if (((IList) new[] {IntToken, BoolToken, StrToken, FloatToken}).Contains(_currentToken.Type))
                return ParseVariableDef();
            return CreateBadStatement("Found assignment symbol, but cannot recognize either assignment or variable definition");
        }

        private Statement ParseAssignment()
        {
            // define functions and variables
            var assignStatement = new AssignmentStatement{LeftSide = _tokens[^3].Value.GetString()};
            Statement retStatement = null;
            var parseNewline = new Func<Statement?>(() =>
            {
                if (_currentToken.Type == NewlineToken)
                {
                    assignStatement.RightSide.SetValue(_tokens[2]);
                    return assignStatement;
                }
                return null;
            });
            var parseBracketExpression = new Func<Statement?>(() =>
            {
                Statement outStatement = null;
                if (ParseBracketExpression(ref assignStatement.RightSide.Type, ref outStatement))
                {
                    if ((SourceEnd || _currentToken.Type == NewlineToken))
                    {
                        if (assignStatement.RightSide.Type == RValue.RValueType.LogicalExpression)
                            assignStatement.RightSide.SetLogicalExpression(_tokens.GetRange(2, _tokens.Count - 3));
                        else
                            assignStatement.RightSide.SetArithmeticExpression(_tokens.GetRange(2, _tokens.Count - 3));
                        return assignStatement;
                    }
                }
                else
                {
                    Log.Error(outStatement.ToString());
                    return CreateBadStatement("Error parsing bracket expression");
                }
                return null;
            });
            // logic 
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant}).Contains(
                _currentToken.Type))
            {
                GetToken();
                if ((retStatement = parseNewline()) != null)
                    return retStatement;
                if ((retStatement = parseBracketExpression()) != null)
                    return retStatement;
            }
            if (_currentToken.Type == Identifier)
            {
                GetToken();
                if ((retStatement = parseNewline()) != null)
                    return retStatement;
                if (_currentToken.Type == LeftParenthesis)
                {
                    var s = ParseFunCall();
                    if (s.Type == FunctionCallType)
                    {
                        var funCall = (FunctionCall) s;
                        assignStatement.RightSide.SetFunCall(funCall);
                        return assignStatement;
                    }
                }
            }
            if ((retStatement = parseBracketExpression()) != null)
                return retStatement;
            return CreateBadStatement("Right side of assignment can be: value, identifier, function call, arithmetic or logical expression");
        }
        
        private Statement ParseVariableDef()
        {
            var varDef = new VariableDef {Name = _tokens[0].Value.GetString(), VariableType = _currentToken.Type};
            GetToken();
            if (_currentToken.Type != LeftParenthesis) return CreateBadStatement("Expected left parenthesis after type specification");
            GetToken();
            if (_currentToken.Type != Identifier)
            {
                switch (varDef.VariableType)
                {
                    case IntToken: if (_currentToken.Type != IntegerConstant) return CreateBadStatement("Should be an integer");
                        break;
                    case FloatToken: if (_currentToken.Type != DecimalConstant) return CreateBadStatement("Should be a float value");
                        break;
                    case BoolToken: if (_currentToken.Type != LogicalConstant) return CreateBadStatement("Should be a logical value");
                        break;
                    case StrToken: if (_currentToken.Type != StringLiteral) return CreateBadStatement("Should be of string type");
                        break;
                    default:
                        return CreateBadStatement("Declared and assigned type not matching");
                }
            }
            varDef.InitialValue = _currentToken;
            GetToken();
            if (_currentToken.Type != RightParenthesis) return CreateBadStatement("Expected right parenthesis");
            GetToken();
            return varDef;
        }
        
        private bool ParseBlock(ref List<Statement> statements, int nestingLevel, ref Statement statement)
        {
            nestingLevel++;
            if (_currentToken.Type != NewlineToken)
            {
                statement = CreateBadStatement("Expected newline");
                return false;
            }
            if (_currentToken.Type == NewlineToken)
            {
                for (int i = 0; i < nestingLevel; i++)
                {
                    GetToken();
                    if (_currentToken.Type != TabToken)
                    {
                        if (i > 1)
                            statement = CreateBadStatement("Indentation error");
                        return i == 0 || i == nestingLevel - 1;
                    }
                }
                GetToken();
            }
            statement = GetNextStatement(nestingLevel);
            if (statement == null)
                return true;
            if (statement.Type == BadStatementType)
                return false;
            statements.Add(statement);
            while (true)
            {
                if (_currentToken.Type == NewlineToken)
                {
                    for (int i = 0; i < nestingLevel; i++)
                    {
                        GetToken();
                        if (_currentToken.Type != TabToken)
                        {
                            if (i > 1)
                                statement = CreateBadStatement("Indentation error");
                            return i == 0 || i == nestingLevel - 1;
                        }
                    }
                    GetToken();
                }
                statement = GetNextStatement(nestingLevel);
                if (statement == null)
                    return true;
                if (statement.Type == BadStatementType)
                    return false;
                statements.Add(statement);
            }
        }

        private void GetToken(bool addTokens = true)
        {
            _currentToken = _tokenSource.GetNextToken();
            if (_currentToken.Type == UnknownToken)
                Log.Error($"Unknown token at line:{_currentToken.LineNumber} col:{_currentToken.ColumnNumber}");
            if (_currentToken.Type == End)
                SourceEnd = true;
            else if (addTokens)
                _tokens.Add(_currentToken);
            Log.Debug(
                $"Parser fetched token: {_currentToken} line:{_currentToken.LineNumber} column:{_currentToken.ColumnNumber}");
        }

        private Statement CreateBadStatement(string desc)
        {
            return new BadStatement {BadToken = _currentToken, Description = desc};
        }
       
/* ---------------------------------------------------------------------------------------------------------------------
 * Bracket expression parsing logic
 * ---------------------------------------------------------------------------------------------------------------------
 */
        private bool ParseBracketExpression(ref RValue.RValueType type, ref Statement statement)
        {
            if (SourceEnd || _currentToken.Type == NewlineToken || _currentToken.Type == Colon)
            {
                statement = CreateBadStatement("Empty bracket expression");
                return false;
            }
            int brackets = 0;
            Token lastToken = _tokens[^2];
            while (!SourceEnd && _currentToken.Type != NewlineToken && _currentToken.Type != Colon)
            {
                bool? result;
                if (_currentToken.Type == LeftParenthesis)
                {
                    brackets++;
                    BracketExpressionShouldReturnFalse(true, ref lastToken);
                    continue;
                }
                if ((result = BracketExpressionNextTokenRightParenthesis(ref statement, ref brackets, lastToken)) != null)
                {
                    if (BracketExpressionShouldReturnFalse(result, ref lastToken)) return false;
                    continue;
                }
                if ((result = BracketExpressionNextTokenUnaryOperator(ref type, ref statement, lastToken)) != null)
                {
                    if (BracketExpressionShouldReturnFalse(result, ref lastToken)) return false;
                    continue;
                }
                if ((result = BracketExpressionNextTokenParameter(ref statement, lastToken)) != null)
                {
                    if (BracketExpressionShouldReturnFalse(result, ref lastToken)) return false;
                    continue;
                }
                if ((result = BracketExpressionNextTokenNot(ref type, ref statement, lastToken)) != null)
                {
                    if (BracketExpressionShouldReturnFalse(result, ref lastToken)) return false;
                    continue;
                }
                if (result == null)
                {
                    statement = CreateBadStatement($"{_currentToken} inside bracket expression does not make sense");
                    return false;
                }
            }

            return BracketExpressionEnd(ref statement, lastToken, brackets);
        }

        private bool BracketExpressionEnd(ref Statement statement, Token lastToken, int brackets)
        {
            if (brackets != 0)
            {
                statement = CreateBadStatement($"Number of opening and closing brackets does not match");
                return false;
            }

            if (lastToken.IsUnaryOperator())
            {
                statement = CreateBadStatement($"Expression cannot end with an unary operator");
                return false;
            }
            
            if (lastToken.Type == NotToken)
            {
                statement = CreateBadStatement($"Expression cannot end with a not token");
                return false;
            }

            return true;
        }

        private bool BracketExpressionShouldReturnFalse(bool? subResult, ref Token lastToken)
        {
            if (subResult != null &&  (bool) subResult)
            {
                lastToken = _currentToken;
                GetToken();
                return false;
            }

            return true;
        }

        private bool? BracketExpressionNextTokenRightParenthesis(ref Statement statement, ref int brackets, Token lastToken)
        {
            if (_currentToken.Type == RightParenthesis)
            {
                if (lastToken.Type == NotToken || lastToken.Type == LeftParenthesis || lastToken.IsUnaryOperator())
                {
                    statement = CreateBadStatement($"{_currentToken} is invalid after {lastToken}");
                    return false;
                }
                brackets--;
                return true;
            }
            return null;
        }

        private bool? BracketExpressionNextTokenUnaryOperator(ref RValue.RValueType type, ref Statement statement, Token lastToken)
        {
            if (_currentToken.IsUnaryOperator())
            {
                if (_currentToken.IsArithmeticOperator())
                {
                    if (type == RValue.RValueType.LogicalExpression)
                    {
                        statement = CreateBadStatement("Cannot determine expression type (logical or arithmetic)");
                        return false;
                    }
                    type = RValue.RValueType.ArithmeticExpression;
                }
                else
                {
                    if (type == RValue.RValueType.ArithmeticExpression)
                    {
                        statement = CreateBadStatement("Cannot determine expression type (logical or arithmetic)");
                        return false;
                    }
                    type = RValue.RValueType.LogicalExpression;
                }

                if (lastToken.Type != RightParenthesis && !lastToken.IsParameter())
                {
                    statement = CreateBadStatement($"{_currentToken} is invalid after {lastToken}");
                    return false;
                }

                return true;
            }

            return null;
        }

        private bool? BracketExpressionNextTokenParameter(ref Statement statement, Token lastToken)
        {
            if (_currentToken.IsParameter())
            {
                if (lastToken.Type != LeftParenthesis &&
                    lastToken.Type != NotToken &&
                    lastToken.Type != WhileToken &&
                    lastToken.Type != IfToken &&
                    !lastToken.IsUnaryOperator())
                {
                    statement = CreateBadStatement($"{_currentToken} is invalid after {lastToken}");
                    return false;
                }

                return true;
            }

            return null;
        }

        private bool? BracketExpressionNextTokenNot(ref RValue.RValueType type, ref Statement statement, Token lastToken)
        {
            if (_currentToken.Type == NotToken)
            {
                if (type == RValue.RValueType.ArithmeticExpression)
                {
                    statement = CreateBadStatement("Cannot determine expression type (logical or arithmetic)");
                    return false;
                }
                type = RValue.RValueType.LogicalExpression;
                if (lastToken.Type != LeftParenthesis && !lastToken.IsUnaryOperator() && lastToken.Type != AssignmentSymbol)
                {
                    statement = CreateBadStatement($"{_currentToken} is invalid after {lastToken}");
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}