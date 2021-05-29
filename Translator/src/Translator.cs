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
            statementIterator.MoveNext();
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

        public static string Imports(string sourceCode)
        {
            sourceCode += "using System.IO;\n";
            sourceCode += "using System;\n";
            sourceCode += "\n";
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

        public static string TranslateFunctionDef(string sourceCode, FunctionDef functionDef, int nestingLevel)
        {
            Log.Information(functionDef.ToString());
            string line = "static ";
            line = TranslateVarType(line, functionDef.ReturnType);
            line += " ";
            line += functionDef.Name;
            line += "(";
            var argIterator = functionDef.ArgList.GetEnumerator();
            argIterator.MoveNext();
            line = TranslateFunctionArg(line, argIterator.Current);
            // args
            line += ")";
            sourceCode = AddLine(sourceCode, line, nestingLevel);
            sourceCode = AddLine(sourceCode, "{", nestingLevel);
            // statements
            foreach (var statement in functionDef.Statements)
            {
                var t = typeof(int);
                switch (statement.Type)
                {
                    case ReturnStatementType:
                        line = "return ";
                        var returnStatement = (ReturnStatement) statement;
                        if (returnStatement.Value.Type == TokenType.Identifier)
                            line += returnStatement.Value.Value.GetString();
                        else if (returnStatement.Value.Value.Type == typeof(bool))
                            line += returnStatement.Value.Value.GetBool() ? "true" : "false";
                        else if (returnStatement.Value.Value.Type == typeof(string))
                            line += $"\"{returnStatement.Value.Value.GetString()}\"";
                        else if (returnStatement.Value.Value.Type == typeof(double))
                            line += returnStatement.Value.Value.GetDouble();
                        else if (returnStatement.Value.Value.Type == typeof(int))
                            line += returnStatement.Value.Value.GetInt();
                        line += ";";
                        sourceCode = AddLine(sourceCode, line, nestingLevel + 1);
                        break;
                }
            }
            sourceCode = AddLine(sourceCode, "}", nestingLevel);
            return sourceCode;
        }

        public static string TranslateFunctionArg(string line, Tuple<string, TokenType> arg)
        {
            line = TranslateVarType(line, arg.Item2);
            line += $" {arg.Item1}";
            return line;
        }

        public static string TranslateVarType(string line, TokenType? type)
        {
            switch (type)
            {
                case null:
                    line += "void";
                    break;
                case TokenType.BoolToken:
                    line += "bool";
                    break;
                case TokenType.IntToken:
                    line += "int";
                    break;
                case TokenType.StrToken:
                    line += "string";
                    break;
                case TokenType.FloatToken:
                    line += "float";
                    break;
            }
            return line;
        }

        public static string FunctionDefs(string sourceCode, ref IEnumerator<Statement> statementIterator)
        {
            while (statementIterator.Current?.Type == FunctionDefType)
            {
                switch (statementIterator.Current.Type)
                {
                    case FunctionDefType:
                        sourceCode = TranslateFunctionDef(sourceCode, (FunctionDef) statementIterator.Current, 1);
                        break;
                }
                statementIterator.MoveNext();
            }
            return sourceCode;
        }

        public static string MainFunction(string sourceCode, ref IEnumerator<Statement> statementIterator)
        {
            sourceCode += "\tstatic void Main(string[] args)\n";
            sourceCode += "\t{\n";
            do
            {
                Log.Information(statementIterator.Current?.ToString());
            } while (statementIterator.MoveNext());
            sourceCode += "\t}\n";
            return sourceCode;
        }
        
        

        public static void Save(string translatedProgram, string filepath)
        {
            var writer = new StreamWriter(filepath);
            writer.Write(translatedProgram);
            writer.Close();
        }

        private static string AddLine(string sourceCode, string line, int nestingLevel)
        {
            for (int i = 0; i < nestingLevel; i++)
            {
                sourceCode += "\t";
            }
            sourceCode += line;
            sourceCode += "\n";
            return sourceCode;
        }
    }
}