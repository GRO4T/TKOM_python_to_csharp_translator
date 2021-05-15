using System;
using System.Collections;
using System.Collections.Generic;

namespace PythonCSharpTranslator 
{
    public enum StatementType
    {
        FunctionCallType,
        IfStatementType,
        WhileLoopType,
        ForLoopType,
        FunctionDefType,
        AssignmentStatementType,
        VariableDefType,
        ReturnStatement,
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

    public class FunctionDef : Statement
    {
        public FunctionDef()
        {
            Type = StatementType.FunctionDefType;
        }

        public string Name;
        public TokenType? ReturnType = null;
        public List<Tuple<string, TokenType>> ArgList = new();
        public List<Statement> Statements = new();
    }

    public class AssignmentStatement : Statement
    {
        public AssignmentStatement()
        {
            Type = StatementType.AssignmentStatementType;
        }
    
        public string LeftSide;
        public RValue RightSide = new();
    }

    public class VariableDef : Statement
    {
        public VariableDef()
        {
            Type = StatementType.VariableDefType;
        }

        public String Name;
        public Token InitialValue;
        public TokenType VariableType;
    }

    public class IfStatement : Statement
    {
        public IfStatement()
        {
            Type = StatementType.IfStatementType;
        }

        public List<Token> Condition = new();
        public List<Statement> Statements = new();
    }

    public class FunctionCall : Statement
    {
        public FunctionCall()
        {
            Type = StatementType.FunctionCallType;
        }

        public String Name;
        public List<Token> Args = new();
    }

    public class ForLoop : Statement
    {
        public ForLoop()
        {
            Type = StatementType.ForLoopType;
        }
    
        public String IteratorName;
        public int Start;
        public int End;
        public List<Statement> Statements = new();
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