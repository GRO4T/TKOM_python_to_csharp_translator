using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public static class Translator
    {
        public static string Translate(ProgramObject program)
        {
            if (program.Statements.Count == 0)
            {
                Log.Information("Program is empty. Exiting...");
                return "";
            }
            ReorderStatements(ref program.Statements);
            IEnumerator<Statement> statementIterator = program.Statements.GetEnumerator();
            string sourceCode = "";
            Log.Information("Starting translation...");
            sourceCode = Imports(sourceCode);
            sourceCode += "internal static class Program\n";
            sourceCode += "{\n";
            sourceCode = FunctionDefs(sourceCode, ref statementIterator);
            sourceCode = MainFunction(sourceCode, ref statementIterator);
            sourceCode += "}\n";
            Log.Information("Translation finished.");
            return sourceCode;
        }
        
        public static void Save(string translatedProgram, string filepath)
        {
            var writer = new StreamWriter(filepath);
            writer.Write(translatedProgram);
            writer.Close();
        }
        
        public static void ReorderStatements(ref List<Statement> statements)
        {
             statements.Sort(delegate(Statement a, Statement b)
             {
                 if (a.Type == FunctionDefType && b.Type != FunctionDefType)
                     return -1;
                 if (a.Type != FunctionDefType && b.Type == FunctionDefType)
                     return 1;
                 return 0;
             });
        }

        private static string Imports(string sourceCode)
        {
            sourceCode += "using System.IO;\n";
            sourceCode += "using System;\n";
            sourceCode += "\n";
            return sourceCode;
        }

        private static string FunctionDefs(string sourceCode, ref IEnumerator<Statement> statementIterator)
        {
            while (statementIterator.MoveNext() && statementIterator.Current?.Type == FunctionDefType)
            {
                sourceCode = TranslateFunctionDef(sourceCode, (FunctionDef) statementIterator.Current, 1);
            }
            return sourceCode;
        }

        private static string MainFunction(string sourceCode, ref IEnumerator<Statement> statementIterator)
        {
            sourceCode += "\tstatic void Main(string[] args)\n";
            sourceCode += "\t{\n";
            try
            {
                do
                {
                    sourceCode = TranslateTypesThatCanOccurOnEveryNestingLevel(sourceCode, statementIterator.Current, 2);
                } while (statementIterator.MoveNext());
            } catch (NullReferenceException e) {}
            sourceCode += "\t}\n";
            return sourceCode;
        }


        private static string TranslateFunctionDef(string sourceCode, FunctionDef functionDef, int nestingLevel)
        {
            Log.Information(functionDef.ToString());
            string line = "static ";
            line = TranslateVarType(line, functionDef.ReturnType);
            line += " ";
            line += functionDef.Name;
            // args
            line += "(";
            using var argIterator = functionDef.ArgList.GetEnumerator();
            if (argIterator.MoveNext())
                line = TranslateFunctionArg(line, argIterator.Current);
            while (argIterator.MoveNext())
            {
                line += ", ";
                line = TranslateFunctionArg(line, argIterator.Current);
            }
            line += ")";
            sourceCode = AddLine(sourceCode, line, nestingLevel, false);
            sourceCode = TranslateBlock(sourceCode, functionDef.Statements, nestingLevel);
            return sourceCode;
        }

        private static string TranslateReturnStatement(string sourceCode, ReturnStatement returnStatement,
            int nestingLevel)
        {
            string line = "return ";
            line = TranslateIdentifierOrConstant(line, returnStatement.Value);
            return AddLine(sourceCode, line, nestingLevel);
        }

        private static string TranslateAssignment(string sourceCode, AssignmentStatement assignmentStatement, int nestingLevel)
        {
            string rvalue = TranslateRValue("", assignmentStatement.RightSide); 
            return AddLine(sourceCode, $"{assignmentStatement.LeftSide} = {rvalue}", nestingLevel);
        }
        
        private static string TranslateFunctionCall(string sourceCode, FunctionCall functionCall)
        {
            sourceCode += $"{functionCall.Name}(";
            using var argIterator = functionCall.Args.GetEnumerator();
            if (argIterator.MoveNext())
                sourceCode = TranslateIdentifierOrConstant(sourceCode, argIterator.Current);
            while (argIterator.MoveNext())
            {
                sourceCode += ", ";
                sourceCode = TranslateIdentifierOrConstant(sourceCode, argIterator.Current);
            }
            sourceCode += ")";
            return sourceCode;
        }

        private static string TranslateVariableDef(string sourceCode, VariableDef variableDef, int nestingLevel)
        {
            string line = "";
            line = TranslateVarType(line, variableDef.VariableType);
            line += $" {variableDef.Name} = ";
            line = TranslateIdentifierOrConstant(line, variableDef.InitialValue);
            return AddLine(sourceCode, line, nestingLevel);
        }

        private static string TranslateIfStatement(string sourceCode, IfStatement ifStatement, int nestingLevel)
        {
            string line = "if ";
            line = TranslateBracketExpression(line, ifStatement.Condition);
            sourceCode = AddLine(sourceCode, line, nestingLevel, false);
            sourceCode = TranslateBlock(sourceCode, ifStatement.Statements, nestingLevel);
            return sourceCode;
        }

        private static string TranslateWhileStatement(string sourceCode, WhileLoop whileLoop, int nestingLevel)
        {
            string line = "while ";
            line = TranslateBracketExpression(line, whileLoop.Condition);
            sourceCode = AddLine(sourceCode, line, nestingLevel, false);
            sourceCode = TranslateBlock(sourceCode, whileLoop.Statements, nestingLevel);
            return sourceCode;
        }

        private static string TranslateForLoop(string sourceCode, ForLoop forLoop, int nestingLevel)
        {
            sourceCode = AddLine(
                sourceCode,
                $"for (int {forLoop.IteratorName} = {forLoop.Start}; {forLoop.IteratorName} < {forLoop.End}; {forLoop.IteratorName}++)",
                nestingLevel,
                false);
            sourceCode = TranslateBlock(sourceCode, forLoop.Statements, nestingLevel);
            return sourceCode;
        }

        private static string TranslateConstantDef(string sourceCode, ConstantDef constantDef, int nestingLevel)
        {
            string line = "const ";
            line = TranslateVarType(line, constantDef.ConstantType);
            line += $" {constantDef.Name} = ";
            line = TranslateIdentifierOrConstant(line, constantDef.RightSide.GetValue());
            sourceCode = AddLine(sourceCode, line, nestingLevel);
            return sourceCode;
        }

        private static string TranslateRValue(string sourceCode, RValue rValue)
        {
            if (rValue.Type == RValue.RValueType.FunCall)
                sourceCode = TranslateFunctionCall(sourceCode, rValue.GetFunCall());
            else if (rValue.Type == RValue.RValueType.Value)
                sourceCode = TranslateIdentifierOrConstant(sourceCode, rValue.GetValue());
            else
                sourceCode = TranslateBracketExpression(sourceCode, rValue.GetLogicalExpression(), false);
            return sourceCode;
        }

        private static string TranslateBracketExpression(string sourceCode, List<Token> tokens, bool addBrackets = true)
        {
            if (addBrackets && (tokens[0].Type != TokenType.LeftParenthesis || tokens[^1].Type != TokenType.RightParenthesis))
                sourceCode += "(";
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.StringLiteral)
                    sourceCode += $"\"{token.Value.GetString()}\"";
                else if (token.IsUnaryOperator())
                    sourceCode += $" {token.Value.GetString()} ";
                else if (token.IsParameter())
                    sourceCode = TranslateIdentifierOrConstant(sourceCode, token);
                else
                    sourceCode += token.Value.GetString();
            }
            if (addBrackets && (tokens[0].Type != TokenType.LeftParenthesis || tokens[^1].Type != TokenType.RightParenthesis))
                sourceCode += ")";
            return sourceCode;
        }
        
        
        private static string TranslateIdentifierOrConstant(string sourceCode, Token token)
        {
            if (token.Type == TokenType.Identifier)
                sourceCode += token.Value.GetString();
            else if (token.Value.Type == typeof(bool))
                sourceCode += token.Value.GetBool() ? "true" : "false";
            else if (token.Value.Type == typeof(string))
                sourceCode += $"\"{token.Value.GetString()}\"";
            else if (token.Value.Type == typeof(double))
                sourceCode += token.Value.GetDouble();
            else if (token.Value.Type == typeof(int))
                sourceCode += token.Value.GetInt();
            return sourceCode;
        }

        private static string TranslateFunctionArg(string sourceCode, Tuple<string, TokenType> arg)
        {
            sourceCode = TranslateVarType(sourceCode, arg.Item2);
            sourceCode += $" {arg.Item1}";
            return sourceCode;
        }


        private static string TranslateVarType(string sourceCode, TokenType? type)
        {
            switch (type)
            {
                case null:
                    sourceCode += "void";
                    break;
                case TokenType.BoolToken:
                    sourceCode += "bool";
                    break;
                case TokenType.IntToken:
                    sourceCode += "int";
                    break;
                case TokenType.StrToken:
                    sourceCode += "string";
                    break;
                case TokenType.FloatToken:
                    sourceCode += "float";
                    break;
            }
            return sourceCode;
        }

        private static string TranslateBlock(string sourceCode, List<Statement> statements, int nestingLevel)
        {
            sourceCode = AddLine(sourceCode, "{", nestingLevel, false);
            foreach (var statement in statements)
            {
                switch (statement.Type)
                {
                    case ReturnStatementType:
                        sourceCode = TranslateReturnStatement(sourceCode, (ReturnStatement) statement, nestingLevel + 1);
                        break;
                    default:
                        sourceCode = TranslateTypesThatCanOccurOnEveryNestingLevel(sourceCode, statement, nestingLevel + 1);
                        break;
                }
            }
            sourceCode = AddLine(sourceCode, "}", nestingLevel, false);
            return sourceCode;
        }

        private static string TranslateTypesThatCanOccurOnEveryNestingLevel(string sourceCode, Statement statement,
            int nestingLevel)
        {
            switch (statement.Type)
            {
                case FunctionCallType:
                    sourceCode = AddLine(sourceCode,
                        TranslateFunctionCall("", (FunctionCall) statement), nestingLevel);
                    break;
                case AssignmentStatementType:
                    sourceCode = TranslateAssignment(sourceCode, (AssignmentStatement) statement, nestingLevel);
                    break;
                case VariableDefType:
                    sourceCode = TranslateVariableDef(sourceCode, (VariableDef) statement, nestingLevel);
                    break;
                case IfStatementType:
                    sourceCode = TranslateIfStatement(sourceCode, (IfStatement) statement, nestingLevel);
                    break;
                case ForLoopType:
                    sourceCode = TranslateForLoop(sourceCode, (ForLoop) statement, nestingLevel);
                    break;
                case WhileLoopType:
                    sourceCode = TranslateWhileStatement(sourceCode, (WhileLoop) statement, nestingLevel);
                    break;
                case ConstantDefType:
                    sourceCode = TranslateConstantDef(sourceCode, (ConstantDef) statement, nestingLevel);
                    break;
                default:
                    throw new TranslationError($"{statement.Type} should not appear inside a nested code block!");
            }
            return sourceCode;
        }

        private static string AddLine(string sourceCode, string line, int nestingLevel, bool endWithColon = true)
        {
            for (int i = 0; i < nestingLevel; i++)
            {
                sourceCode += "\t";
            }
            sourceCode += line;
            if (endWithColon)
                sourceCode += ";\n";
            else
                sourceCode += "\n";
            return sourceCode;
        }
    }
}