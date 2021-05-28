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
            sourceCode = FunctionDefs(sourceCode, ref statementIterator);
            sourceCode += "internal static class Program\n";
            sourceCode += "{\n";
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

        public static string Translate(string sourceCode, AssignmentStatement statement)
        {
            return sourceCode;
        }

        public static string FunctionDefs(string sourceCode, ref IEnumerator<Statement> statementIterator)
        {
            while (statementIterator.Current?.Type == FunctionDefType)
            {
                Log.Information(statementIterator.Current?.ToString());
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
    }
}