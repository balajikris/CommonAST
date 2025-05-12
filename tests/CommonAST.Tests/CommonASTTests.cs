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