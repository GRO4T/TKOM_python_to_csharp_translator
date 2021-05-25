using System.IO;

namespace PythonCSharpTranslator
{
    public class Translator
    {
        public static string Translate(ProgramObject program)
        {
            return "Hello Translator";
        }

        public static void Save(string translatedProgram, string filepath)
        {
            var writer = new StreamWriter(filepath);
            writer.Write(translatedProgram);
            writer.Close();
        }
    }
}