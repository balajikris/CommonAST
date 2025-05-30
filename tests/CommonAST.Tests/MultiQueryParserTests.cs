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

        // Demo test cases showcasing powerful expression support
        [TestMethod]
        public void Demo1_ComplexLogicalExpressions_ShowcasesPowerfulFiltering()
        {
            // Arrange - Complex logical expressions with AND/OR operators
            string input = "T | where (trace.duration > 1s and trace.status == 'error') or (trace.service == 'api' and trace.method == 'POST') $$ << L | where (span.name contains 'database' and span.duration > 100ms) or span.tags['user.id'] == '12345' >>";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(1, filterNode.SpanFilter.Expressions.Count);
        }

        [TestMethod]
        public void Demo2_ArithmeticAndStringOperations_ShowcasesExpressionPower()
        {
            // Arrange - Arithmetic expressions and string operations
            string input = "T | where trace.duration > (500 + 1000) and trace.service startswith 'micro' $$ << L | where span.duration * 2 > 1000 and span.name endswith 'Handler' >>";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
        }

        [TestMethod]
        public void Demo3_MultipleSpanFiltersWithComplexConditions_ShowcasesAdvancedFiltering()
        {
            // Arrange - Multiple span filters with different conditions
            string input = "T | where trace.status == 'success' $$ << L | where span.service == 'auth' and span.http_status_code >= 200 and span.http_status_code < 300 $$ L | where span.service == 'payment' and span.tags['transaction.amount'] > 1000 $$ L | where span.name matches 'process.*request' >>";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(3, filterNode.SpanFilter.Expressions.Count);
        }

        [TestMethod]
        public void Demo4_TimeBasedFilteringWithDateFunctions_ShowcasesTemporalQueries()
        {
            // Arrange - Time-based filtering with date functions
            string input = "T | where trace.start_time >= ago(1h) and trace.duration between (100ms .. 5s) $$ << L | where span.start_time >= datetime('2023-01-01') and span.service in ('frontend', 'api', 'backend') >>";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
        }

        [TestMethod]
        public void Demo5_ComplexTraceAndSpanFilterCombinations_ShowcasesFullPower()
        {
            // Arrange - Complex combinations of trace and span filters
            string input = "T | where trace.root_service == 'gateway' and trace.span_count > 10 and trace.error_count == 0 $$ << L | where span.parent_id != '' and span.tags['region'] in ('us-east-1', 'us-west-2') and span.duration > percentile(span.duration, 95) $$ L | where span.service == 'database' and span.operation_name startswith 'SELECT' and span.tags['db.statement'] contains 'WHERE' >>";

            // Act
            var queryNode = MultiQueryParser.Parse(input);

            // Assert
            Assert.IsNotNull(queryNode);
            Assert.AreEqual(1, queryNode.Operations.Count);
            var filterNode = (FilterNode)queryNode.Operations[0];
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(2, filterNode.SpanFilter.Expressions.Count);
        }
    }
}
