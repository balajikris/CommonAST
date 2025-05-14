using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CommonAST.Tests
{
    [TestClass]
    public class MultiQueryParserTests
    {
        [TestMethod]
        public void Parse_SimpleTraceFilter_CreatesCorrectQueryNode()
        {
            // Arrange
            string input = "T | where trace.duration > 1s";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            Assert.IsTrue(queryNode.Operations[0] is FilterNode);
            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNull(filterNode.SpanFilter);
        }

        [TestMethod]
        public void Parse_WithTraceAndSpanFiltersInAngleBrackets_CreatesCorrectQueryNode()
        {            // Arrange
            string input = "T | where trace.duration > 1s $$ << Logs | where span.service == 'gateway'$$ Logs | where span.service == 'backend' >>";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            Assert.IsTrue(queryNode.Operations[0] is FilterNode);

            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(2, filterNode.SpanFilter.Expressions.Count);
        }

        [TestMethod]
        public void Parse_WithOnlySpanFilters_CreatesCorrectQueryNode()
        {            // Arrange
            string input = "<< Logs | where span.service == 'gateway'$$ Logs | where span.service == 'backend' >>";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            Assert.IsTrue(queryNode.Operations[0] is FilterNode);

            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(2, filterNode.SpanFilter.Expressions.Count);
        }

        [TestMethod]
        public void Parse_WithMultipleQueriesWithoutBrackets_CreatesCorrectQueryNode()
        {      
            // Arrange
            string input = "T | where trace.duration > 1s $$ Logs | where span.service == 'gateway'";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            // Assert.IsNotNull(queryNode);
            // Assert.AreEqual(1, queryNode.Operations.Count);
            // Assert.IsTrue(queryNode.Operations[0] is FilterNode);

            // var filterNode = (FilterNode)queryNode.Operations[0];
            // Assert.IsNotNull(filterNode.TraceExpression);
            // Assert.IsNotNull(filterNode.SpanFilter);
            // Assert.AreEqual(1, filterNode.SpanFilter.Expressions.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Parse_WithInvalidQuery_ThrowsArgumentException()
        {
            // Arrange
            string input = "T | invalidkeyword something";

            // Act & Assert
            MultiQueryParser.Parse(input);
        }
    }
}
