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
        BadStatementType
    }
    
    public class Statement
    {

        public StatementType Type;
        
        public override string ToString()
        {
            return $"{Type}";
        }
    }

    public class AssignmentStatement : Statement
    {
        public AssignmentStatement()
        {
            Type = StatementType.AssignmentStatementType;
        }
    
        public Token LeftSide;
        public RValue RightSide;
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

    public class BadStatement : Statement
    {
        public BadStatement()
        {
            Type = StatementType.BadStatementType;
        }
    
        public Token BadToken;
    }
}