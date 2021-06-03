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
        ConstantDefType,
        ReturnStatementType,
        BadStatementType
    }

    public class Statement
    {

        public StatementType Type;
        public int NestingLevel = 0;
        private int _lineNumber = 0;

        public int LineNumber
        {
            get
            {
                if (_lineNumber == 0)
                    throw new Exception("Statement line number is 0!");
                return _lineNumber;
            }
            set => _lineNumber = value;
        }
        
        public override string ToString()
        {
            string indent = "";
            for (int i = 0; i < NestingLevel; i++)
                indent += '\t';
            return $"{indent}{Type}";
        }

        public virtual string GetName()
        {
            return null;
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

        public override string GetName()
        {
            return Name;
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
        
        public override string GetName()
        {
            return LeftSide;
        }
    
        public string LeftSide;
        public RValue RightSide = new();
    }

    public class ConstantDef : Statement
    {
        public ConstantDef()
        {
            Type = StatementType.ConstantDefType;
        }
        
        public override string GetName()
        {
            return Name;
        }
        
        public string Name;
        public RValue RightSide = new();
        public TokenType ConstantType;
    }

    public class VariableDef : Statement
    {
        public VariableDef()
        {
            Type = StatementType.VariableDefType;
        }
        
        public override string GetName()
        {
            return Name;
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
        
        public override string GetName()
        {
            return Name;
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
            return $"{Description} at line:{lineNum} col:{colNum} error:{BadToken.ErrorDescription}";
        }
    }
}