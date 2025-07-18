using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommonAST;

namespace CommonAST.Tests
{
    [TestClass]
    public class CommonASTTests
    {
        #region Basic Node Creation Tests
        
        [TestMethod]
        public void CreateQuery_WithSource_CreatesQueryNodeCorrectly()
        {
            
            // Arrange & Act
            var query = AstBuilder.CreateQuery("TestSource");
            
            // Assert
            Assert.IsNotNull(query);
            Assert.AreEqual(NodeKind.Query, query.NodeKind);
            Assert.AreEqual("TestSource", query.Source);
            Assert.IsNotNull(query.Operations);
            Assert.AreEqual(0, query.Operations.Count);
        }
        
        [TestMethod]
        public void CreateQueryWithOperations_AddsOperationsCorrectly()
        {
            // Arrange
            var operations = new List<OperationNode>
            {
                AstBuilder.CreateFilter(
                    AstBuilder.CreateBinaryExpression(
                        AstBuilder.CreateIdentifier("test"),
                        BinaryOperatorKind.Equal,
                        AstBuilder.CreateLiteral(42, LiteralKind.Integer)
                    )
                )
            };
            
            // Act
            var query = AstBuilder.CreateQueryWithOperations(operations, "TestSource");
            
            // Assert
            Assert.IsNotNull(query);
            Assert.AreEqual("TestSource", query.Source);
            Assert.AreEqual(1, query.Operations.Count);
            Assert.IsInstanceOfType(query.Operations[0], typeof(FilterNode));
        }
        
        [TestMethod]
        public void CreateLiteral_WithValue_CreatesLiteralNodeCorrectly()
        {
            // Arrange & Act
            var literal = AstBuilder.CreateLiteral(42, LiteralKind.Integer);
            
            // Assert
            Assert.IsNotNull(literal);
            Assert.AreEqual(NodeKind.Literal, literal.NodeKind);
            Assert.AreEqual(LiteralKind.Integer, literal.LiteralKind);
            Assert.AreEqual(42, literal.Value);
        }
        
        [TestMethod]
        public void CreateIdentifier_WithNamespace_CreatesIdentifierNodeCorrectly()
        {
            // Arrange & Act
            var identifier = AstBuilder.CreateIdentifier("name", "span");
            
            // Assert
            Assert.IsNotNull(identifier);
            Assert.AreEqual(NodeKind.Identifier, identifier.NodeKind);
            Assert.AreEqual("name", identifier.Name);
            Assert.AreEqual("span", identifier.Namespace);
        }
        
        [TestMethod]
        public void CreateBinaryExpression_CreatesExpressionCorrectly()
        {
            // Arrange 
            var left = AstBuilder.CreateIdentifier("a");
            var right = AstBuilder.CreateLiteral(10, LiteralKind.Integer);
            
            // Act
            var binaryExpr = AstBuilder.CreateBinaryExpression(left, BinaryOperatorKind.GreaterThan, right);
            
            // Assert
            Assert.IsNotNull(binaryExpr);
            Assert.AreEqual(NodeKind.BinaryExpression, binaryExpr.NodeKind);
            Assert.AreEqual(BinaryOperatorKind.GreaterThan, binaryExpr.Operator);
            Assert.AreSame(left, binaryExpr.Left);
            Assert.AreSame(right, binaryExpr.Right);
        }
        
        #endregion
        
        #region Filter Node Tests
        
        [TestMethod]
        public void CreateFilter_WithExpression_CreatesFilterNodeCorrectly()
        {
            // Arrange
            var expression = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("a"),
                BinaryOperatorKind.Equal,
                AstBuilder.CreateLiteral(10, LiteralKind.Integer)
            );
            
            // Act
            var filterNode = AstBuilder.CreateFilter(expression, "where");
            
            // Assert
            Assert.IsNotNull(filterNode);
            Assert.AreEqual(NodeKind.Filter, filterNode.NodeKind);
            Assert.AreEqual("where", filterNode.Keyword);
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNull(filterNode.SpanFilter);
            Assert.AreSame(expression, filterNode.TraceExpression);
            // Test backward compatibility property
            Assert.AreSame(expression, filterNode.Expression);
        }
        
        [TestMethod]
        public void FilterExpression_BackwardCompatibility_WorksCorrectly()
        {
            // Arrange
            var expression = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("a"),
                BinaryOperatorKind.Equal,
                AstBuilder.CreateLiteral(10, LiteralKind.Integer)
            );
            
            // Act
            var filterNode = new FilterNode();
            filterNode.Expression = expression;
            
            // Assert
            Assert.AreSame(expression, filterNode.TraceExpression);
            Assert.AreSame(expression, filterNode.Expression);
        }
        
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FilterExpression_Get_ThrowsWhenBothExpressionsNull()
        {
            // Arrange
            var filterNode = new FilterNode();
            
            // Act - This should throw
            var _ = filterNode.Expression;
        }
        
        #endregion
        
        #region Span Filter Tests
        
        [TestMethod]
        public void CreateCombinedFilter_WithTraceAndSpanExpressions_CreatesFilterNodeCorrectly()
        {
            // Arrange
            var traceExpression = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("duration", "trace"),
                BinaryOperatorKind.GreaterThan,
                AstBuilder.CreateLiteral("1s", LiteralKind.Duration)
            );
            
            var spanExpressions = new List<Expression>
            {
                AstBuilder.CreateBinaryExpression(
                    AstBuilder.CreateIdentifier("name", "span"),
                    BinaryOperatorKind.Equal,
                    AstBuilder.CreateLiteral("db", LiteralKind.String)
                ),
                AstBuilder.CreateBinaryExpression(
                    AstBuilder.CreateIdentifier("status", "span"),
                    BinaryOperatorKind.Equal,
                    AstBuilder.CreateLiteral("ERROR", LiteralKind.String)
                )
            };
            
            // Act
            var filterNode = AstBuilder.CreateCombinedFilter(
                traceExpression,
                spanExpressions,
                SpanFilterCombination.Any,
                "where"
            );
            
            // Assert
            Assert.IsNotNull(filterNode);
            Assert.AreEqual(NodeKind.Filter, filterNode.NodeKind);
            Assert.AreEqual("where", filterNode.Keyword);
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreSame(traceExpression, filterNode.TraceExpression);
            Assert.AreEqual(2, filterNode.SpanFilter.Expressions.Count);
            Assert.AreEqual(SpanFilterCombination.Any, filterNode.SpanFilter.Combination);
        }
        
        [TestMethod]
        public void CreateCombinedFilter_WithNullSpanExpressions_CreatesFilterWithoutSpanFilter()
        {
            // Arrange
            var traceExpression = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("duration", "trace"),
                BinaryOperatorKind.GreaterThan,
                AstBuilder.CreateLiteral("1s", LiteralKind.Duration)
            );
            
            // Act
            var filterNode = AstBuilder.CreateCombinedFilter(traceExpression, null);
            
            // Assert
            Assert.IsNotNull(filterNode);
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNull(filterNode.SpanFilter);
        }
        
        [TestMethod]
        public void CreateCombinedFilter_WithEmptySpanExpressions_CreatesFilterWithoutSpanFilter()
        {
            // Arrange
            var traceExpression = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("duration", "trace"),
                BinaryOperatorKind.GreaterThan,
                AstBuilder.CreateLiteral("1s", LiteralKind.Duration)
            );
            
            // Act
            var filterNode = AstBuilder.CreateCombinedFilter(traceExpression, new List<Expression>());
            
            // Assert
            Assert.IsNotNull(filterNode);
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNull(filterNode.SpanFilter);
        }
        
        [TestMethod]
        public void CreateSpanFilter_WithExpressions_CreatesFilterWithOnlySpanFilter()
        {
            // Arrange
            var spanExpressions = new List<Expression>
            {
                AstBuilder.CreateBinaryExpression(
                    AstBuilder.CreateIdentifier("status", "span"),
                    BinaryOperatorKind.Equal,
                    AstBuilder.CreateLiteral("ERROR", LiteralKind.String)
                )
            };
            
            // Act
            var filterNode = AstBuilder.CreateSpanFilter(
                spanExpressions, 
                SpanFilterCombination.All,
                "where"
            );
            
            // Assert
            Assert.IsNotNull(filterNode);
            Assert.AreEqual("where", filterNode.Keyword);
            Assert.IsNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(SpanFilterCombination.All, filterNode.SpanFilter.Combination);
            Assert.AreEqual(1, filterNode.SpanFilter.Expressions.Count);
        }
        
        #endregion
        
        #region Advanced Expression Types Tests
        
        [TestMethod]
        public void CreateUnaryExpression_WithNegation_CreatesCorrectly()
        {
            // Arrange
            var operand = AstBuilder.CreateLiteral(42, LiteralKind.Integer);
            
            // Act
            var unaryExpr = AstBuilder.CreateUnaryExpression("-", operand);
            
            // Assert
            Assert.IsNotNull(unaryExpr);
            Assert.AreEqual(NodeKind.UnaryExpression, unaryExpr.NodeKind);
            Assert.AreEqual("-", unaryExpr.Operator);
            Assert.AreSame(operand, unaryExpr.Argument);
        }
        
        [TestMethod]
        public void CreateCallExpression_WithMultipleArguments_CreatesCorrectly()
        {
            // Arrange
            var args = new List<Expression>
            {
                AstBuilder.CreateIdentifier("field1"),
                AstBuilder.CreateLiteral(100, LiteralKind.Integer),
                AstBuilder.CreateLiteral("test", LiteralKind.String)
            };
            
            // Act
            var callExpr = AstBuilder.CreateCallExpression("max", args);
            
            // Assert
            Assert.IsNotNull(callExpr);
            Assert.AreEqual(NodeKind.CallExpression, callExpr.NodeKind);
            Assert.AreEqual("max", callExpr.Callee.Name);
            Assert.AreEqual(3, callExpr.Arguments.Count);
            Assert.AreSame(args[0], callExpr.Arguments[0]);
            Assert.AreSame(args[1], callExpr.Arguments[1]);
            Assert.AreSame(args[2], callExpr.Arguments[2]);
        }
        
        [TestMethod]
        public void CreateSpecialOperatorExpression_WithInOperator_CreatesCorrectly()
        {
            // Arrange
            var left = AstBuilder.CreateIdentifier("status");
            var right = new List<Expression>
            {
                AstBuilder.CreateLiteral("active", LiteralKind.String),
                AstBuilder.CreateLiteral("pending", LiteralKind.String),
                AstBuilder.CreateLiteral("processing", LiteralKind.String)
            };
            
            // Act
            var specialExpr = AstBuilder.CreateSpecialOperatorExpression(left, SpecialOperatorKind.In, right);
            
            // Assert
            Assert.IsNotNull(specialExpr);
            Assert.AreEqual(NodeKind.SpecialOperatorExpression, specialExpr.NodeKind);
            Assert.AreEqual(SpecialOperatorKind.In, specialExpr.Operator);
            Assert.AreSame(left, specialExpr.Left);
            Assert.AreEqual(3, specialExpr.Right.Count);
        }
        
        [TestMethod]
        public void CreateSpecialOperatorExpression_WithBetweenOperator_CreatesCorrectly()
        {
            // Arrange
            var left = AstBuilder.CreateIdentifier("age");
            var right = new List<Expression>
            {
                AstBuilder.CreateLiteral(18, LiteralKind.Integer),
                AstBuilder.CreateLiteral(65, LiteralKind.Integer)
            };
            
            // Act
            var specialExpr = AstBuilder.CreateSpecialOperatorExpression(left, SpecialOperatorKind.Between, right);
            
            // Assert
            Assert.IsNotNull(specialExpr);
            Assert.AreEqual(SpecialOperatorKind.Between, specialExpr.Operator);
            Assert.AreEqual(2, specialExpr.Right.Count);
        }
        
        #endregion
        
        #region Complex Nested Expression Tests
        
        [TestMethod]
        public void CreateNestedBinaryExpressions_WithLogicalOperators_CreatesCorrectly()
        {
            // Arrange - Create (a > 10 AND b < 20) OR (c == 'test')
            var aGreaterThan10 = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("a"),
                BinaryOperatorKind.GreaterThan,
                AstBuilder.CreateLiteral(10, LiteralKind.Integer)
            );
            
            var bLessThan20 = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("b"),
                BinaryOperatorKind.LessThan,
                AstBuilder.CreateLiteral(20, LiteralKind.Integer)
            );
            
            var cEqualsTest = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("c"),
                BinaryOperatorKind.Equal,
                AstBuilder.CreateLiteral("test", LiteralKind.String)
            );
            
            var andExpression = AstBuilder.CreateBinaryExpression(aGreaterThan10, BinaryOperatorKind.And, bLessThan20);
            
            // Act
            var orExpression = AstBuilder.CreateBinaryExpression(andExpression, BinaryOperatorKind.Or, cEqualsTest);
            
            // Assert
            Assert.IsNotNull(orExpression);
            Assert.AreEqual(BinaryOperatorKind.Or, orExpression.Operator);
            Assert.AreEqual(NodeKind.BinaryExpression, orExpression.Left.NodeKind);
            Assert.AreEqual(NodeKind.BinaryExpression, orExpression.Right.NodeKind);
            
            // Verify the nested AND expression
            var leftAndExpr = orExpression.Left as BinaryExpression;
            Assert.IsNotNull(leftAndExpr);
            Assert.AreEqual(BinaryOperatorKind.And, leftAndExpr.Operator);
        }
        
        [TestMethod]
        public void CreateExpressionWithFunctionCall_InBinaryExpression_CreatesCorrectly()
        {
            // Arrange - Create duration > max(threshold, 100)
            var maxCall = AstBuilder.CreateCallExpression("max", new List<Expression>
            {
                AstBuilder.CreateIdentifier("threshold"),
                AstBuilder.CreateLiteral(100, LiteralKind.Integer)
            });
            
            // Act
            var binaryExpr = AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("duration"),
                BinaryOperatorKind.GreaterThan,
                maxCall
            );
            
            // Assert
            Assert.IsNotNull(binaryExpr);
            Assert.AreEqual(NodeKind.Identifier, binaryExpr.Left.NodeKind);
            Assert.AreEqual(NodeKind.CallExpression, binaryExpr.Right.NodeKind);
            
            var rightCall = binaryExpr.Right as CallExpression;
            Assert.IsNotNull(rightCall);
            Assert.AreEqual("max", rightCall.Callee.Name);
            Assert.AreEqual(2, rightCall.Arguments.Count);
        }
        
        #endregion
        
        #region Literal Type Coverage Tests
        
        [TestMethod]
        public void CreateLiteral_WithAllLiteralTypes_CreatesCorrectly()
        {
            // Test all literal types
            var stringLiteral = AstBuilder.CreateLiteral("test", LiteralKind.String);
            var intLiteral = AstBuilder.CreateLiteral(42, LiteralKind.Integer);
            var floatLiteral = AstBuilder.CreateLiteral(3.14, LiteralKind.Float);
            var boolLiteral = AstBuilder.CreateLiteral(true, LiteralKind.Boolean);
            var nullLiteral = AstBuilder.CreateLiteral(null, LiteralKind.Null);
            var durationLiteral = AstBuilder.CreateLiteral("1h30m", LiteralKind.Duration);
            var dateTimeLiteral = AstBuilder.CreateLiteral("2023-01-01T00:00:00Z", LiteralKind.DateTime);
            var guidLiteral = AstBuilder.CreateLiteral(Guid.NewGuid(), LiteralKind.Guid);
            
            // Assert all types
            Assert.AreEqual(LiteralKind.String, stringLiteral.LiteralKind);
            Assert.AreEqual(LiteralKind.Integer, intLiteral.LiteralKind);
            Assert.AreEqual(LiteralKind.Float, floatLiteral.LiteralKind);
            Assert.AreEqual(LiteralKind.Boolean, boolLiteral.LiteralKind);
            Assert.AreEqual(LiteralKind.Null, nullLiteral.LiteralKind);
            Assert.AreEqual(LiteralKind.Duration, durationLiteral.LiteralKind);
            Assert.AreEqual(LiteralKind.DateTime, dateTimeLiteral.LiteralKind);
            Assert.AreEqual(LiteralKind.Guid, guidLiteral.LiteralKind);
        }
        
        #endregion
        
        #region Edge Cases and Error Conditions
        
        [TestMethod]
        public void CreateFilter_WithNullTraceAndSpanExpressions_ThrowsOrHandlesGracefully()
        {
            // Act & Assert - This should either throw or handle gracefully
            var filterNode = AstBuilder.CreateCombinedFilter(null, null);
            
            // Should create a valid filter node even with null expressions
            Assert.IsNotNull(filterNode);
            Assert.IsNull(filterNode.TraceExpression);
            Assert.IsNull(filterNode.SpanFilter);
        }
        
        [TestMethod]
        public void SpanFilter_WithMixedExpressionTypes_HandlesCorrectly()
        {
            // Arrange - Mix different expression types in span filter
            var expressions = new List<Expression>
            {
                // Binary expression
                AstBuilder.CreateBinaryExpression(
                    AstBuilder.CreateIdentifier("status"),
                    BinaryOperatorKind.Equal,
                    AstBuilder.CreateLiteral("ERROR", LiteralKind.String)
                ),
                // Special operator expression
                AstBuilder.CreateSpecialOperatorExpression(
                    AstBuilder.CreateIdentifier("service"),
                    SpecialOperatorKind.In,
                    new List<Expression>
                    {
                        AstBuilder.CreateLiteral("auth", LiteralKind.String),
                        AstBuilder.CreateLiteral("payment", LiteralKind.String)
                    }
                ),
                // Function call
                AstBuilder.CreateCallExpression("duration", new List<Expression>
                {
                    AstBuilder.CreateIdentifier("span")
                })
            };
            
            // Act
            var filterNode = AstBuilder.CreateSpanFilter(expressions, SpanFilterCombination.All);
            
            // Assert
            Assert.IsNotNull(filterNode);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(3, filterNode.SpanFilter.Expressions.Count);
            Assert.AreEqual(SpanFilterCombination.All, filterNode.SpanFilter.Combination);
            
            // Verify different expression types
            Assert.AreEqual(NodeKind.BinaryExpression, filterNode.SpanFilter.Expressions[0].NodeKind);
            Assert.AreEqual(NodeKind.SpecialOperatorExpression, filterNode.SpanFilter.Expressions[1].NodeKind);
            Assert.AreEqual(NodeKind.CallExpression, filterNode.SpanFilter.Expressions[2].NodeKind);
        }
        
        [TestMethod]
        public void CreateQuery_WithComplexOperationPipeline_CreatesCorrectly()
        {
            // Arrange - Create a query with multiple operations
            var operations = new List<OperationNode>
            {
                // First filter
                AstBuilder.CreateFilter(
                    AstBuilder.CreateBinaryExpression(
                        AstBuilder.CreateIdentifier("timestamp"),
                        BinaryOperatorKind.GreaterThan,
                        AstBuilder.CreateLiteral("2023-01-01", LiteralKind.DateTime)
                    ),
                    "where"
                ),
                // Combined filter with trace and span
                AstBuilder.CreateCombinedFilter(
                    AstBuilder.CreateBinaryExpression(
                        AstBuilder.CreateIdentifier("duration", "trace"),
                        BinaryOperatorKind.GreaterThan,
                        AstBuilder.CreateLiteral("1s", LiteralKind.Duration)
                    ),
                    new List<Expression>
                    {
                        AstBuilder.CreateBinaryExpression(
                            AstBuilder.CreateIdentifier("status", "span"),
                            BinaryOperatorKind.Equal,
                            AstBuilder.CreateLiteral("ERROR", LiteralKind.String)
                        )
                    }
                )
            };
            
            // Act
            var query = AstBuilder.CreateQueryWithOperations(operations, "TraceData");
            
            // Assert
            Assert.IsNotNull(query);
            Assert.AreEqual("TraceData", query.Source);
            Assert.AreEqual(2, query.Operations.Count);
            
            // Verify first operation
            var firstFilter = query.Operations[0] as FilterNode;
            Assert.IsNotNull(firstFilter);
            Assert.IsNotNull(firstFilter.TraceExpression);
            Assert.IsNull(firstFilter.SpanFilter);
            
            // Verify second operation
            var secondFilter = query.Operations[1] as FilterNode;
            Assert.IsNotNull(secondFilter);
            Assert.IsNotNull(secondFilter.TraceExpression);
            Assert.IsNotNull(secondFilter.SpanFilter);
        }
        
        #endregion

        #region Example Tests
        
        [TestMethod]
        public void KqlWhereExample_CreatesExpectedAst()
        {
            // Arrange & Act
            var query = Examples.KqlWhereExample();
            
            // Assert
            Assert.IsNotNull(query);
            Assert.AreEqual("MyTable", query.Source);
            Assert.AreEqual(1, query.Operations.Count);
            
            var filterNode = query.Operations[0] as FilterNode;
            Assert.IsNotNull(filterNode);
            Assert.AreEqual("where", filterNode.Keyword);
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNull(filterNode.SpanFilter);
        }
        
        [TestMethod]
        public void TraceQLFilterExample_CreatesExpectedAst()
        {
            // Arrange & Act
            var query = Examples.TraceQLFilterExample();
            
            // Assert
            Assert.IsNotNull(query);
            Assert.IsNull(query.Source);
            Assert.AreEqual(1, query.Operations.Count);
            
            var filterNode = query.Operations[0] as FilterNode;
            Assert.IsNotNull(filterNode);
            Assert.IsNull(filterNode.Keyword);
            Assert.IsNotNull(filterNode.TraceExpression);
            
            var binExpr = filterNode.TraceExpression as BinaryExpression;
            Assert.IsNotNull(binExpr);
            
            var left = binExpr.Left as Identifier;
            Assert.IsNotNull(left);
            Assert.AreEqual("duration", left.Name);
            Assert.AreEqual("span", left.Namespace);
        }
        
        [TestMethod]
        public void CombinedFilteringExample_CreatesExpectedAst()
        {
            // Arrange & Act
            var query = Examples.CombinedFilteringExample();
            
            // Assert
            Assert.IsNotNull(query);
            Assert.AreEqual(1, query.Operations.Count);
            
            var filterNode = query.Operations[0] as FilterNode;
            Assert.IsNotNull(filterNode);
            Assert.IsNotNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            
            // Check trace expression
            var traceExpr = filterNode.TraceExpression as BinaryExpression;
            Assert.IsNotNull(traceExpr);
            
            var traceLeft = traceExpr.Left as Identifier;
            Assert.IsNotNull(traceLeft);
            Assert.AreEqual("duration", traceLeft.Name);
            Assert.AreEqual("trace", traceLeft.Namespace);
            
            // Check span filter
            Assert.AreEqual(SpanFilterCombination.Any, filterNode.SpanFilter.Combination);
            Assert.AreEqual(2, filterNode.SpanFilter.Expressions.Count);
            
            // Check first span expression
            var spanExpr1 = filterNode.SpanFilter.Expressions[0] as BinaryExpression;
            Assert.IsNotNull(spanExpr1);
            
            var spanLeft1 = spanExpr1.Left as Identifier;
            Assert.IsNotNull(spanLeft1);
            Assert.AreEqual("name", spanLeft1.Name);
            Assert.AreEqual("span", spanLeft1.Namespace);
            
            var spanRight1 = spanExpr1.Right as Literal;
            Assert.IsNotNull(spanRight1);
            Assert.AreEqual("db", spanRight1.Value);
        }
        
        [TestMethod]
        public void SpansOnlyFilterExample_CreatesExpectedAst()
        {
            // Arrange & Act
            var query = Examples.SpansOnlyFilterExample();
            
            // Assert
            Assert.IsNotNull(query);
            Assert.AreEqual(1, query.Operations.Count);
            
            var filterNode = query.Operations[0] as FilterNode;
            Assert.IsNotNull(filterNode);
            Assert.IsNull(filterNode.TraceExpression);
            Assert.IsNotNull(filterNode.SpanFilter);
            Assert.AreEqual(SpanFilterCombination.All, filterNode.SpanFilter.Combination);
            Assert.AreEqual(2, filterNode.SpanFilter.Expressions.Count);
            
            // Verify first expression (status = ERROR)
            var spanExpr1 = filterNode.SpanFilter.Expressions[0] as BinaryExpression;
            Assert.IsNotNull(spanExpr1);
            Assert.AreEqual(BinaryOperatorKind.Equal, spanExpr1.Operator);
            
            var spanLeft1 = spanExpr1.Left as Identifier;
            Assert.IsNotNull(spanLeft1);
            Assert.AreEqual("status", spanLeft1.Name);
            Assert.AreEqual("span", spanLeft1.Namespace);
            
            // Verify second expression (duration > 200ms)
            var spanExpr2 = filterNode.SpanFilter.Expressions[1] as BinaryExpression;
            Assert.IsNotNull(spanExpr2);
            Assert.AreEqual(BinaryOperatorKind.GreaterThan, spanExpr2.Operator);
            
            var spanLeft2 = spanExpr2.Left as Identifier;
            Assert.IsNotNull(spanLeft2);
            Assert.AreEqual("duration", spanLeft2.Name);
            Assert.AreEqual("span", spanLeft2.Namespace);
            
            var spanRight2 = spanExpr2.Right as Literal;
            Assert.IsNotNull(spanRight2);
            Assert.AreEqual("200ms", spanRight2.Value);
        }
        
        #endregion
    }
}