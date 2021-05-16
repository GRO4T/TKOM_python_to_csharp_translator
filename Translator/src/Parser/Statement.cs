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
        ReturnStatementType,
        BadStatementType
    }

    public class Statement
    {

        public StatementType Type;
        public int NestingLevel = 0;
        
        public override string ToString()
        {
            string indent = "";
            for (int i = 0; i < NestingLevel; i++)
                indent += '\t';
            return $"{indent}{Type}";
        }
    }

    public class BlockStatement : Statement
    {
        public List<Statement> Statements = new();

        public override string ToString()
        {
            string childrenToString = "";
            foreach (var statement in Statements)
            {
                statement.NestingLevel = NestingLevel + 1;
                childrenToString += statement.ToString() + '\n';
            }
            return $"{base.ToString()}\n{childrenToString}";
        }
    }

    public class FunctionDef : BlockStatement
    {
        public FunctionDef()
        {
            Type = StatementType.FunctionDefType;
        }

        public string Name;
        public TokenType? ReturnType = null;
        public List<Tuple<string, TokenType>> ArgList = new();
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

    public class IfStatement : BlockStatement
    {
        public IfStatement()
        {
            Type = StatementType.IfStatementType;
        }

        public List<Token> Condition = new();
    }
    
    public class WhileLoop : BlockStatement 
    {
        public WhileLoop()
        {
            Type = StatementType.WhileLoopType;
        }

        public List<Token> Condition = new();
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

    public class ForLoop : BlockStatement 
    {
        public ForLoop()
        {
            Type = StatementType.ForLoopType;
        }
    
        public String IteratorName;
        public int Start;
        public int End;
    }

    public class ReturnStatement : Statement
    {
        public ReturnStatement()
        {
            Type = StatementType.ReturnStatementType;
        }
        public Token Value;
    }

    public class BadStatement : Statement
    {
        public BadStatement()
        {
            Type = StatementType.BadStatementType;
        }
    
        public Token BadToken;
        public string Description;

        public override string ToString()
        {
            var colNum = BadToken.ColumnNumber == -1 ? "last" : BadToken.ColumnNumber.ToString();
            var lineNum = BadToken.ColumnNumber == -1 ? BadToken.LineNumber - 1 : BadToken.LineNumber;
            return $"{Description} at line:{lineNum} col:{colNum}";
        }
    }
}