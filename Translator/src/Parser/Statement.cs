namespace PythonCSharpTranslator 
{
    public class Statement
    {
        public enum Type
        {
            FunctionCall,
            IfStatement,
            WhileLoop,
            ForLoop,
            FunctionDef,
            AssignmentStatement,
            VariableDef,
        }
    }
}