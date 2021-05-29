using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public static class Translator
    {
        public static string Translate(ProgramObject program)
        {
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
        
        public static void ReorderStatements(ref List<Statement> statements)
        {
             statements.Sort(delegate(Statement a, Statement b)
             {
                 if (a.Type == b.Type)
                     return 0;
                 if (a.Type == FunctionDefType)
                     return -1;
                 return 1;
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
            do
            {
                switch (statementIterator.Current.Type)
                {
                    case AssignmentStatementType:
                        sourceCode = TranslateAssignment(sourceCode, (AssignmentStatement) statementIterator.Current,
                            2);
                        break;
                    case FunctionCallType:
                        sourceCode = AddLine(sourceCode,
                            TranslateFunctionCall("", (FunctionCall) statementIterator.Current), 2);
                        break;
                    case VariableDefType:
                        sourceCode = TranslateVariableDef(sourceCode, (VariableDef) statementIterator.Current,  2);
                        break;
                    case ForLoopType:
                        sourceCode = TranslateForLoop(sourceCode, (ForLoop) statementIterator.Current, 2);
                        break;
                }
                Log.Information(statementIterator.Current?.ToString());
            } while (statementIterator.MoveNext());
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
            string rvalue = TranslateIdentifierOrConstant("", assignmentStatement.RightSide.GetValue());
            return AddLine(sourceCode, $"{assignmentStatement.LeftSide} = {rvalue}", nestingLevel);
        }
        
        private static string TranslateFunctionCall(string sourceCode, FunctionCall functionCall)
        {
            sourceCode += $"{functionCall.Name}(";
            using var argIterator = functionCall.Args.GetEnumerator();
            if (argIterator.MoveNext())
                sourceCode += argIterator.Current.Value.GetString();
            while (argIterator.MoveNext())
                sourceCode += $", {argIterator.Current.Value.GetString()}";
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
        
        
        private static string TranslateIdentifierOrConstant(string line, Token token)
        {
            if (token.Type == TokenType.Identifier)
                line += token.Value.GetString();
            else if (token.Value.Type == typeof(bool))
                line += token.Value.GetBool() ? "true" : "false";
            else if (token.Value.Type == typeof(string))
                line += $"\"{token.Value.GetString()}\"";
            else if (token.Value.Type == typeof(double))
                line += token.Value.GetDouble();
            else if (token.Value.Type == typeof(int))
                line += token.Value.GetInt();
            return line;
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
            // statements
            foreach (var statement in statements)
            {
                var t = typeof(int);
                switch (statement.Type)
                {
                    case ReturnStatementType:
                        sourceCode = TranslateReturnStatement(sourceCode, (ReturnStatement) statement, nestingLevel + 1);
                        break;
                    case AssignmentStatementType:
                        sourceCode = TranslateAssignment(sourceCode, (AssignmentStatement) statement, nestingLevel + 1);
                        break;
                    case VariableDefType:
                        sourceCode = TranslateVariableDef(sourceCode, (VariableDef) statement, nestingLevel + 1);
                        break;
                    default:
                        throw new TranslationError($"{statement.Type} should not appear inside a function definition!");
                }
            }
            sourceCode = AddLine(sourceCode, "}", nestingLevel, false);
            return sourceCode;
        }

        
        

        public static void Save(string translatedProgram, string filepath)
        {
            var writer = new StreamWriter(filepath);
            writer.Write(translatedProgram);
            writer.Close();
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