using System.Collections.Generic;
using PythonCSharpTranslator;
using Xunit;
using static PythonCSharpTranslator.StatementType;

namespace Tests
{
    public class TestTranslator
    {
        [Theory]
        [InlineData(new []{AssignmentStatementType, FunctionDefType}, new []{FunctionDefType, AssignmentStatementType})]
        
        public void TestReorderStatements(StatementType[] inStatementTypes, StatementType[] outStatementTypes)
        {
            var inStatements = new List<Statement>();
            foreach (var statementType in inStatementTypes)
            {
                inStatements.Add(new Statement{Type = statementType});
            }
            Translator.ReorderStatements(ref inStatements);
            for (int i = 0; i < inStatements.Count; i++)
            {
                Assert.Equal(outStatementTypes[i], inStatements[i].Type); 
            }
        }
    }
}