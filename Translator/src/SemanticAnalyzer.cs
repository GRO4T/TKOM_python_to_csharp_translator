using System.Collections.Generic;
using System.Transactions;
using Serilog;
using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public class SemanticAnalyzer
    {
        class Context
        {
            public Context() {}
            public Context(Context context)
            {
                SymbolTable = new Dictionary<string, Statement>(context.SymbolTable);
                ReturnType = context.ReturnType;
            } 
            
            public Dictionary<string, Statement> SymbolTable = new();
            public TokenType? ReturnType = null;
        }
        
        private readonly Parser _parser;
        private Context _globalContext = new();
        
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
            return EvaluateStatement(_parser.GetNextStatement(), ref _globalContext);
        }

        private Statement EvaluateStatement(Statement statement, ref Context context)
        {
            if (statement == null) return null;
            if (statement.Type == BadStatementType)
            {
                Log.Error(statement.ToString());
                throw new TranslationError();
            }
            EvaluateStatementIfHasName(ref statement, ref context);
            EvaluateStatementIfDoesNotHaveName(statement, context);
            Log.Information($"Fetched statement:\n {statement}");
            return statement;
        }

        private void EvaluateStatementIfHasName(ref Statement statement, ref Context context)
        {
            string name;
            if ((name = statement.GetName()) != null)
            {
                if (context.SymbolTable.ContainsKey(name))
                {
                    if (statement.Type == VariableDefType || statement.Type == FunctionDefType)
                        throw new TranslationError($"Symbol {name} already declared", statement.LineNumber);
                    EvaluateIfAssignmentAndSymbolDeclared(statement, context.SymbolTable[name], context);
                }
                else
                {
                    EvaluateIfAssignmentAndNotDeclared(ref statement);
                    if (statement.Type == FunctionCallType)
                        throw new TranslationError($"Function {name} not declared", statement.LineNumber);
                    if (statement.Type == FunctionDefType)
                        EvaluateFunctionDef((FunctionDef) statement, context);
                    context.SymbolTable.Add(name, statement);
                }
            }
        }

        private void EvaluateStatementIfDoesNotHaveName(Statement statement, Context context)
        {
            if (statement.GetName() == null)
            {
                EvaluateIf_IfStatement(statement, context); 
                EvaluateIfWhileLoop(statement, context);
                EvaluateIfForLoop(statement, context);
            }
        }

        private void EvaluateIfAssignmentAndSymbolDeclared(Statement statement, Statement declaredAs, Context context)
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
                    var assignedType = EvaluateRValue(assignment.RightSide, context).ValueType;
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
        
        private RValue EvaluateRValue(RValue rValue, Context context)
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
                        if (!context.SymbolTable.ContainsKey(identifierName))
                            throw new TranslationError($"Symbol {identifierName} not declared");
                        else
                        {
                            var symbol = context.SymbolTable[identifierName];
                            if (symbol.Type != VariableDefType)
                                throw new TranslationError($"Symbol declared as {symbol.Type} used as VariableDefType");
                            var variableDef = (VariableDef) symbol;
                            rValue.ValueType = variableDef.VariableType;
                        }
                    }
                    break;
                case RValue.RValueType.FunCall:
                    var funCall = rValue.GetFunCall();
                    EvaluateFunctionCall(funCall, context);
                    var returnType = ((FunctionDef) context.SymbolTable[funCall.Name]).ReturnType;
                    if (returnType != null)
                        rValue.ValueType = (TokenType) returnType;
                    break;
                case RValue.RValueType.ArithmeticExpression:
                    rValue.ValueType = EvaluateArithmeticExpression(rValue.GetArithmeticExpression(), context);
                    break;
                case RValue.RValueType.LogicalExpression:
                    EvaluateLogicalExpression(rValue.GetLogicalExpression(), context);
                    rValue.ValueType = TokenType.BoolToken;
                    break;
            }
            
            return rValue;
        }

        private void PrintTokenList(List<Token> tokenList)
        {
            string line = "";
            foreach (var token in tokenList)
            {
                line += $"{token.ToString()} ";
                // line += token.Value.ToString();
            }
            Log.Information(line);
        }
        private void EvaluateLogicalExpression(List<Token> expression, Context context)
        {
            int i = 0;
            while (i < expression.Count)
            {
                var token = expression[i];
                if (token.IsParameter())
                {
                    EvaluateRValue(new RValue(token), context);
                }
                else if (token.Type == TokenType.NotToken)
                {
                    i++;
                    var rvalue = EvaluateRValue(new RValue(expression[i]), context);
                    if (rvalue.ValueType != TokenType.BoolToken)
                        throw new TranslationError("Cannot negate non-boolean value", token.LineNumber);
                }
                else if (token.IsUnaryOperator())
                {
                    EvaluateUnaryOperator(i, expression, context);
                }
                i++;
            }
        }

        private void EvaluateUnaryOperator(int index, List<Token> expression, Context context)
        {
            TokenType leftSideType = TokenType.BoolToken;
            TokenType rightSideType = TokenType.BoolToken;
            var op = expression[index];
            if (expression[index - 1].IsParameter()) // 1 and ..
                leftSideType = EvaluateRValue(new RValue(expression[index - 1]), context).ValueType;
            else // (1) and ..
            {
                int x = 2;
                while (!expression[index - x].IsParameter())
                    x++;
                if (!expression[index - x - 1].IsUnaryOperator())
                    leftSideType = EvaluateRValue(new RValue(expression[index - x]), context).ValueType;
            }
            if (expression[index + 1].IsParameter()) // ... and 1
                rightSideType = EvaluateRValue(new RValue(expression[index + 1]), context).ValueType;
            else // ... and (1)
            {
                int x = 2;
                while (!expression[index + x].IsParameter())
                    x++;
                if (!expression[index + x + 1].IsUnaryOperator())
                    rightSideType = EvaluateRValue(new RValue(expression[index + x]), context).ValueType;
            } 
            if ((op.Type == TokenType.AndToken || op.Type == TokenType.OrToken) && (leftSideType != TokenType.BoolToken || rightSideType != TokenType.BoolToken))
            {
                throw new TranslationError("Both operands should be of bool type", op.LineNumber);
            }
            else if (leftSideType != rightSideType)
            {
                throw new TranslationError("Both operands should be of the same type", op.LineNumber);
            }
        }

        private void EvaluateIf_IfStatement(Statement statement, Context context)
        {
            if (statement.Type == IfStatementType)
            {
                var ifStatement = (IfStatement) statement;
                EvaluateLogicalExpression(ifStatement.Condition, context);
                EvaluateBlock(ifStatement.Statements, context);
            }
        }

        private void EvaluateIfWhileLoop(Statement statement, Context context)
        {
            if (statement.Type == WhileLoopType)
            {
                var whileLoop = (WhileLoop) statement;
                EvaluateLogicalExpression(whileLoop.Condition, context); 
                EvaluateBlock(whileLoop.Statements, context);
            }
        }

        private void EvaluateIfForLoop(Statement statement, Context context)
        {
            if (statement.Type == ForLoopType)
            {
                var forLoop = (ForLoop) statement;
                var localContext = new Context(context);
                localContext.SymbolTable[forLoop.IteratorName] = new VariableDef {VariableType = TokenType.IntToken};
                EvaluateBlock(forLoop.Statements, localContext);
            }
        }

        private TokenType EvaluateArithmeticExpression(List<Token> expression, Context context)
        {
            TokenType expressionType = TokenType.UnknownToken;
            foreach (var token in expression)
            {
                if (token.IsParameter())
                {
                    var rvalue = EvaluateRValue(new RValue(token), context);
                    if (expressionType == TokenType.UnknownToken)
                        expressionType = rvalue.ValueType;
                    else if (rvalue.ValueType != expressionType)
                        throw new TranslationError("Cannot determine arithmetic expression type", expression[0].LineNumber);
                }
            }
            return expressionType;
        }

        private void EvaluateFunctionCall(FunctionCall functionCall, Context context)
        {
            if (!context.SymbolTable.ContainsKey(functionCall.Name))
                throw new TranslationError($"Function {functionCall.Name} not declared", functionCall.LineNumber);
            else
            {
                var symbol = context.SymbolTable[functionCall.Name];
                if (symbol.Type != FunctionDefType)
                    throw new TranslationError($"Symbol declared as {symbol.Type} but used as {functionCall.Type}", functionCall.LineNumber);
                var functionDef = (FunctionDef) symbol;
                if (functionCall.Args.Count != functionDef.ArgList.Count)
                    throw new TranslationError($"Number of arguments does not match", functionCall.LineNumber);
                for (int i = 0; i < functionCall.Args.Count; i++)
                {
                    var gotToken = functionCall.Args[i];
                    var expectedType = functionDef.ArgList[i].Item2;
                    EvaluateRValue(new RValue(gotToken), context);
                    TokenType gotTokenType = TokenType.UnknownToken;
                    if (gotToken.IsConstantValue())
                        gotTokenType = EvaluateConstantType(gotToken.Type);
                    else
                        gotTokenType = ((VariableDef)context.SymbolTable[gotToken.Value.GetString()]).VariableType;
                    if (gotTokenType != expectedType)
                        throw new TranslationError(
                            $"Argument number {i + 1}. Types do not match. Expected {expectedType}. Got {gotTokenType}",
                            functionCall.LineNumber);
                }
            }
        }

        private void EvaluateFunctionDef(FunctionDef functionDef, Context context)
        {
            var localContext = new Context(context);
            foreach (var arg in functionDef.ArgList)
            {
                localContext.SymbolTable[arg.Item1] = new VariableDef {VariableType = arg.Item2};
            }
            localContext.ReturnType = functionDef.ReturnType;
            EvaluateBlock(functionDef.Statements, localContext);
        }

        private void EvaluateBlock(List<Statement> statements, Context context)
        {
            var localContext = new Context(context);
            for (int i = 0; i < statements.Count; i++)
            {
                statements[i] = EvaluateStatement(statements[i], ref localContext);
                if (statements[i].Type == ReturnStatementType)
                {
                    var returnStatement = (ReturnStatement) statements[i];
                    var foundType = EvaluateRValue(new RValue(returnStatement.Value), localContext).ValueType;
                    if (context.ReturnType == null)
                        throw new TranslationError($"Function should not return a value", statements[i].LineNumber);
                    if (foundType != context.ReturnType)
                        throw new TranslationError(
                            $"Wrong return type. Got {foundType}. Expected {context.ReturnType}", statements[i].LineNumber);
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