using System;
using System.Collections;
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
        AssignmentStatementType,
        VariableDefType,
        UnknownStatement
    }
    
    public class Statement
    {

        public StatementType Type;
        public List<Token> Tokens = new();
        
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

    public class AssignmentStatement : Statement
    {
        public AssignmentStatement()
        {
            Type = StatementType.AssignmentStatementType;
        }
    
        public Token LeftSide;
        public Token RightSide;
    }

    public class VariableDef : Statement
    {
        public VariableDef()
        {
            Type = StatementType.VariableDefType;
        }
        
        public Token LeftSide;
        public Token RightSide;
        public TokenType VariableType;
    }

    public class FunCall : Statement
    {
        public FunCall()
        {
            Type = StatementType.FunctionCall;
        }

        public String Name;
        public TokenType ReturnType;

        public List<Token> Args = new();
    }
}