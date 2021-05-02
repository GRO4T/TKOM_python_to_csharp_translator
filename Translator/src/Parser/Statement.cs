using System.Collections.Generic;

namespace PythonCSharpTranslator 
{
    public enum StatementType
    {
        FunctionCall,
        IfStatement,
        WhileLoop,
        ForLoop,
        FunctionDef,
        AssignmentStatement,
        VariableDef,
        UnknownStatement
    }
    
    public class Statement
    {

        public StatementType Type;
        public List<Token> Tokens;
        
        public override string ToString()
        {
            string tokenStrings = "";
            Tokens.ForEach(delegate(Token token)
            {
                tokenStrings += token.ToString() + ", ";
            });
            return $"{Type}:( {tokenStrings} )";
        }
    }
}