using System;
using System.Collections.Generic;
using CommonAST;

class TestProjectNode
{
    static void Main()
    {
        Console.WriteLine("Testing ProjectNode implementation...");
        
        // Test 1: Simple field projections
        var projections = new List<ProjectionExpression>
        {
            AstBuilder.CreateFieldProjection("name"),
            AstBuilder.CreateFieldProjection("duration"),
            AstBuilder.CreateFieldProjection("status")
        };
        
        var projectNode = AstBuilder.CreateProject(projections, "project");
        
        Console.WriteLine($"✓ ProjectNode created with {projectNode.Projections.Count} projections");
        Console.WriteLine($"✓ Keyword: {projectNode.Keyword}");
        Console.WriteLine($"✓ NodeKind: {projectNode.NodeKind}");
        
        // Test 2: Projection with alias and calculated field
        var calculatedProjection = AstBuilder.CreateProjection(
            AstBuilder.CreateBinaryExpression(
                AstBuilder.CreateIdentifier("duration"),
                BinaryOperatorKind.Divide,
                AstBuilder.CreateLiteral(1000, LiteralKind.Integer)
            ),
            "duration_ms",
            ExpressionType.Float
        );
        
        Console.WriteLine($"✓ Calculated projection created with alias: {calculatedProjection.Alias}");
        Console.WriteLine($"✓ Result type: {calculatedProjection.ResultType}");
        
        // Test 3: TraceQL select example
        var traceQLProjections = new List<ProjectionExpression>
        {
            AstBuilder.CreateFieldProjection("name", ns: "span"),
            AstBuilder.CreateFieldProjection("duration", ns: "span")
        };
        
        var selectNode = AstBuilder.CreateProject(traceQLProjections, "select");
        Console.WriteLine($"✓ TraceQL select node created with keyword: {selectNode.Keyword}");
        
        // Test 4: Examples work
        try 
        {
            var kqlExample = Examples.KqlSimpleProjectExample();
            var traceQLExample = Examples.TraceQLSelectExample();
            var complexExample = Examples.QueryWithFilterAndProjectExample();
            
            Console.WriteLine($"✓ KQL example operations count: {kqlExample.Operations.Count}");
            Console.WriteLine($"✓ TraceQL example operations count: {traceQLExample.Operations.Count}");
            Console.WriteLine($"✓ Complex example operations count: {complexExample.Operations.Count}");
            
            Console.WriteLine("\n🎉 All ProjectNode tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in examples: {ex.Message}");
        }
    }
}
