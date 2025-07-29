using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommonAST;

namespace CommonAST.Tests
{
    [TestClass]
    public class AggregationTests
    {
        #region Phase 1: Basic AST Node Tests

        [TestMethod]
        public void CreateFieldReference_WithNamespace_CreatesCorrectly()
        {
            // Arrange & Act
            var fieldRef = AstBuilder.CreateFieldReference("Duration", "event", FieldType.Attribute, DataType.Integer);

            // Assert
            Assert.IsNotNull(fieldRef);
            Assert.AreEqual(NodeKind.FieldReference, fieldRef.NodeKind);
            Assert.AreEqual("Duration", fieldRef.Name);
            Assert.AreEqual("event", fieldRef.Namespace);
            Assert.AreEqual(FieldType.Attribute, fieldRef.FieldType);
            Assert.AreEqual(DataType.Integer, fieldRef.DataType);
            Assert.AreEqual(true, fieldRef.IsRequired);
        }

        [TestMethod]
        public void CreateNamedExpression_WithSingleName_CreatesCorrectly()
        {
            // Arrange
            var expr = AstBuilder.CreateIdentifier("count");

            // Act
            var namedExpr = AstBuilder.CreateNamedExpression(expr, "TotalCount");

            // Assert
            Assert.IsNotNull(namedExpr);
            Assert.AreEqual(NodeKind.NamedExpression, namedExpr.NodeKind);
            Assert.AreEqual("TotalCount", namedExpr.Name);
            Assert.IsNull(namedExpr.Names);
            Assert.IsTrue(namedExpr.IsNamed);
            Assert.AreSame(expr, namedExpr.Expression);
        }

        [TestMethod]
        public void CreateAggregateOperation_CountFunction_CreatesCorrectly()
        {
            // Arrange & Act
            var aggOp = AstBuilder.CreateCountOperation("TotalCount");

            // Assert
            Assert.IsNotNull(aggOp);
            Assert.AreEqual(AggregateFunction.Count, aggOp.Function);
            Assert.IsNull(aggOp.Field); // Count doesn't require a field
            Assert.AreEqual("TotalCount", aggOp.ResultName);
            Assert.IsNull(aggOp.SourceExpression);
        }

        [TestMethod]
        public void CreateAggregateOperation_SumFunction_CreatesCorrectly()
        {
            // Arrange
            var field = AstBuilder.CreateFieldReference("Duration", dataType: DataType.Integer);

            // Act
            var aggOp = AstBuilder.CreateSumOperation(field, "TotalDuration");

            // Assert
            Assert.IsNotNull(aggOp);
            Assert.AreEqual(AggregateFunction.Sum, aggOp.Function);
            Assert.AreSame(field, aggOp.Field);
            Assert.AreEqual("TotalDuration", aggOp.ResultName);
            Assert.IsNull(aggOp.SourceExpression);
        }

        [TestMethod]
        public void CreateCompositeAggregation_GroupOnly_CreatesCorrectly()
        {
            // Arrange
            var groupFields = new List<FieldReference>
            {
                AstBuilder.CreateFieldReference("State"),
                AstBuilder.CreateFieldReference("EventType")
            };

            // Act
            var composite = AstBuilder.CreateGroupOnlyAggregation(groupFields, "KQL");

            // Assert
            Assert.IsNotNull(composite);
            Assert.AreEqual(NodeKind.CompositeAggregation, composite.NodeKind);
            Assert.AreEqual(2, composite.GroupByFields.Count);
            Assert.AreEqual(0, composite.Aggregations.Count);
            Assert.AreEqual("KQL", composite.SourceLanguage);
            Assert.IsTrue(composite.IsValid);
            Assert.IsTrue(composite.IsGroupOnly);
            Assert.IsFalse(composite.IsAggregateOnly);
            Assert.IsFalse(composite.IsMixed);
        }

        [TestMethod]
        public void CreateCompositeAggregation_AggregateOnly_CreatesCorrectly()
        {
            // Arrange
            var aggregations = new List<AggregateOperationNode>
            {
                AstBuilder.CreateCountOperation("TotalCount"),
                AstBuilder.CreateSumOperation(AstBuilder.CreateFieldReference("Duration"), "TotalDuration")
            };

            // Act
            var composite = AstBuilder.CreateAggregateOnlyAggregation(aggregations, "KQL");

            // Assert
            Assert.IsNotNull(composite);
            Assert.AreEqual(NodeKind.CompositeAggregation, composite.NodeKind);
            Assert.AreEqual(0, composite.GroupByFields.Count);
            Assert.AreEqual(2, composite.Aggregations.Count);
            Assert.AreEqual("KQL", composite.SourceLanguage);
            Assert.IsTrue(composite.IsValid);
            Assert.IsFalse(composite.IsGroupOnly);
            Assert.IsTrue(composite.IsAggregateOnly);
            Assert.IsFalse(composite.IsMixed);
        }

        [TestMethod]
        public void CreateCompositeAggregation_Mixed_CreatesCorrectly()
        {
            // Arrange
            var groupFields = new List<FieldReference>
            {
                AstBuilder.CreateFieldReference("State")
            };

            var aggregations = new List<AggregateOperationNode>
            {
                AstBuilder.CreateCountOperation("TotalCount"),
                AstBuilder.CreateAverageOperation(AstBuilder.CreateFieldReference("Duration"), "AvgDuration")
            };

            // Act
            var composite = AstBuilder.CreateCompositeAggregation(groupFields, aggregations, "KQL");

            // Assert
            Assert.IsNotNull(composite);
            Assert.AreEqual(NodeKind.CompositeAggregation, composite.NodeKind);
            Assert.AreEqual(1, composite.GroupByFields.Count);
            Assert.AreEqual(2, composite.Aggregations.Count);
            Assert.AreEqual("KQL", composite.SourceLanguage);
            Assert.IsTrue(composite.IsValid);
            Assert.IsFalse(composite.IsGroupOnly);
            Assert.IsFalse(composite.IsAggregateOnly);
            Assert.IsTrue(composite.IsMixed);
        }

        [TestMethod]
        public void CreateCompositeAggregation_EmptyBoth_IsInvalid()
        {
            // Arrange & Act
            var composite = AstBuilder.CreateCompositeAggregation(null, null, "KQL");

            // Assert
            Assert.IsNotNull(composite);
            Assert.AreEqual(0, composite.GroupByFields.Count);
            Assert.AreEqual(0, composite.Aggregations.Count);
            Assert.IsFalse(composite.IsValid); // Must have either grouping OR aggregations
        }

        [TestMethod]
        public void CreateAllAggregateOperations_Phase1Functions_CreateCorrectly()
        {
            // Arrange
            var field = AstBuilder.CreateFieldReference("Duration", dataType: DataType.Integer);

            // Act
            var count = AstBuilder.CreateCountOperation("Count");
            var sum = AstBuilder.CreateSumOperation(field, "Sum");
            var avg = AstBuilder.CreateAverageOperation(field, "Average");
            var min = AstBuilder.CreateMinimumOperation(field, "Minimum");
            var max = AstBuilder.CreateMaximumOperation(field, "Maximum");

            // Assert
            Assert.AreEqual(AggregateFunction.Count, count.Function);
            Assert.IsNull(count.Field);

            Assert.AreEqual(AggregateFunction.Sum, sum.Function);
            Assert.AreSame(field, sum.Field);

            Assert.AreEqual(AggregateFunction.Average, avg.Function);
            Assert.AreSame(field, avg.Field);

            Assert.AreEqual(AggregateFunction.Minimum, min.Function);
            Assert.AreSame(field, min.Field);

            Assert.AreEqual(AggregateFunction.Maximum, max.Function);
            Assert.AreSame(field, max.Field);
        }

        [TestMethod]
        public void CreateKqlMultiAggregationExample_MatchesDesignDoc()
        {
            // Example: | summarize TotalCount = count(), AvgDuration = avg(Duration) by State, EventType
            
            // Arrange
            var groupFields = new List<FieldReference>
            {
                AstBuilder.CreateFieldReference("State"),
                AstBuilder.CreateFieldReference("EventType")
            };

            var aggregations = new List<AggregateOperationNode>
            {
                AstBuilder.CreateCountOperation("TotalCount"),
                AstBuilder.CreateAverageOperation(
                    AstBuilder.CreateFieldReference("Duration", dataType: DataType.Integer), 
                    "AvgDuration")
            };

            // Act
            var composite = AstBuilder.CreateCompositeAggregation(groupFields, aggregations, "KQL");

            // Assert
            Assert.IsNotNull(composite);
            Assert.AreEqual("KQL", composite.SourceLanguage);
            Assert.AreEqual(2, composite.GroupByFields.Count);
            Assert.AreEqual(2, composite.Aggregations.Count);
            Assert.IsTrue(composite.IsMixed);

            // Verify group fields
            Assert.AreEqual("State", composite.GroupByFields[0].Name);
            Assert.AreEqual("EventType", composite.GroupByFields[1].Name);

            // Verify aggregations
            var count = composite.Aggregations[0];
            Assert.AreEqual(AggregateFunction.Count, count.Function);
            Assert.AreEqual("TotalCount", count.ResultName);
            Assert.IsNull(count.Field);

            var avg = composite.Aggregations[1];
            Assert.AreEqual(AggregateFunction.Average, avg.Function);
            Assert.AreEqual("AvgDuration", avg.ResultName);
            Assert.IsNotNull(avg.Field);
            Assert.AreEqual("Duration", avg.Field.Name);
        }

        [TestMethod]
        public void CreateQueryWithCompositeAggregation_AddsToOperations()
        {
            // Arrange
            var query = AstBuilder.CreateQuery("TestTable");
            var composite = AstBuilder.CreateCompositeAggregation(
                new List<FieldReference> { AstBuilder.CreateFieldReference("State") },
                new List<AggregateOperationNode> { AstBuilder.CreateCountOperation("Count") },
                "KQL"
            );

            // Act
            query.Operations.Add(composite);

            // Assert
            Assert.AreEqual("TestTable", query.Source);
            Assert.AreEqual(1, query.Operations.Count);
            Assert.IsInstanceOfType(query.Operations[0], typeof(CompositeAggregationNode));

            var aggNode = query.Operations[0] as CompositeAggregationNode;
            Assert.IsNotNull(aggNode);
            Assert.IsTrue(aggNode.IsMixed);
        }

        #endregion
    }
}
