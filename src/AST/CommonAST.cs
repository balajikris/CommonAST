namespace CommonAST;
using System.Collections.Generic;

/// <summary>
/// Enum representing the type of node in the AST
/// </summary>
public enum NodeKind
{
    Query,
    Filter,
    Project,
    Literal,
    Identifier,
    BinaryExpression,
    UnaryExpression,
    CallExpression,
    ParenthesizedExpression,
    SpecialOperatorExpression,
    WildcardExpression,
    PathExpression,
    ProjectionExpression
}

/// <summary>
/// Base interface for all AST nodes
/// </summary>
public abstract class ASTNode
{
    public abstract NodeKind NodeKind { get; }

    // [BK]: Dropping. Not supporting round trip serialization, error reporting on AST for now.
    // SourceLocation? Location { get; set; }
}

/// <summary>
/// Base class for all operation nodes in a query pipeline
/// </summary>
public abstract class OperationNode : ASTNode
{
}

/// <summary>
/// Root node representing a complete query with a pipeline of operations
/// </summary>
public class QueryNode : ASTNode
{
    public override NodeKind NodeKind => NodeKind.Query;
    
    /// <summary>
    /// List of operations in the query pipeline, executed in order
    /// </summary>
    public List<OperationNode> Operations { get; set; } = new List<OperationNode>();
    
    /// <summary>
    /// Optional source for the query (table name, etc.)
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// Source location information for debugging/error reporting
/// </summary>
// public class SourceLocation
// {
//     public required Position Start { get; set; }
//     public required Position End { get; set; }
// }

/// <summary>
/// Position in source code
/// </summary>
// public class Position
// {
//     public required int Line { get; set; }
//     public required int Column { get; set; }
// }

/// <summary>
/// Filter operation node representing both KQL's whereOperator and TraceQL's SpansetFilter
/// </summary>
public class FilterNode : OperationNode
{
    public override NodeKind NodeKind => NodeKind.Filter;

    // [BK]: Dropping. Not supporting round trip serialization, error reporting on AST for now.
    // public SourceLocation? Location { get; set; }

    // [BK]: Consider Dropping. Not needed for TraceQL, but needed for KQL. Can be dropped, since at this point, we are not supporting round trip serialization.
    public string? Keyword { get; set; } // 'where', 'filter', or null for TraceQL

    // [BK]: Dropping. Needed only for Kusto engine.
    // public List<Parameter>? Parameters { get; set; } // For KQL parameters

    /// <summary>
    /// The expression to filter at the trace level
    /// </summary>
    public Expression? TraceExpression { get; set; }
    
    /// <summary>
    /// Filter for span-level filtering. When null, only trace-level filtering is applied.
    /// </summary>
    public SpanFilter? SpanFilter { get; set; }
    
    /// <summary>
    /// The filter predicate expression. This is for backward compatibility.
    /// Setting this property will set the TraceExpression property.
    /// </summary>
    public Expression Expression
    {
        get => TraceExpression ?? throw new InvalidOperationException("No expression defined");
        set => TraceExpression = value;
    }
}

/// <summary>
/// Container for span-level filtering expressions and their combination mode
/// </summary>
public class SpanFilter
{
    /// <summary>
    /// List of expressions to be applied to spans
    /// </summary>
    public List<Expression> Expressions { get; set; } = new List<Expression>();
    
    /// <summary>
    /// Defines how multiple span filter expressions are combined
    /// </summary>
    public SpanFilterCombination Combination { get; set; } = SpanFilterCombination.Any;
}

/// <summary>
/// Specifies how multiple span filter expressions should be combined
/// </summary>
public enum SpanFilterCombination
{
    /// <summary>
    /// A trace is included if ANY of its spans match the filter expressions (OR semantics)
    /// </summary>
    Any,
    
    /// <summary>
    /// A trace is included only if ALL of its spans match the filter expressions (AND semantics)
    /// </summary>
    All
}

/// <summary>
/// Project operation node representing both KQL's project operator and TraceQL's select operation
/// Projects specified fields/columns from the data, optionally with aliases
/// 
/// Level 1 Support (Current):
/// - Simple field references
/// - Aliasing  
/// - Basic arithmetic operations (+, -, *, /)
/// - Simple function calls (string, math, conversion)
/// 
/// Level 2 Support (Future TODO):
/// - Complex nested expressions
/// - Conditional expressions (case)
/// - Advanced function calls
/// </summary>
public class ProjectNode : OperationNode
{
    public override NodeKind NodeKind => NodeKind.Project;
    
    /// <summary>
    /// List of field projections to include in the output
    /// No wildcard (*) support - if all fields needed, project operation should be omitted
    /// </summary>
    public List<ProjectionExpression> Projections { get; set; } = new List<ProjectionExpression>();
    
    /// <summary>
    /// Keyword for language-specific syntax ('project' for KQL, 'select' for TraceQL)
    /// </summary>
    public string? Keyword { get; set; }
}

/// <summary>
/// Represents a single field projection with optional alias and type information
/// </summary>
public class ProjectionExpression : ASTNode
{
    public override NodeKind NodeKind => NodeKind.ProjectionExpression;
    
    /// <summary>
    /// The expression to project (field name, calculation, function call, etc.)
    /// </summary>
    public required Expression Expression { get; set; }
    
    /// <summary>
    /// Optional alias for the projected field
    /// </summary>
    public string? Alias { get; set; }
    
    /// <summary>
    /// Optional result type of this projection expression
    /// Required ONLY for calculated fields and function calls where type changes
    /// Simple field selections and renames don't need this - EE engine can infer from schema
    /// </summary>
    public ExpressionType? ResultType { get; set; }
}

/// <summary>
/// Parameter for operators (KQL specific)
/// </summary>
// public class Parameter : INode
// {
//     public string NodeType => "Parameter";
//     public SourceLocation? Location { get; set; }
//     public required string Name { get; set; }
//     public required INode Value { get; set; } // Literal or Identifier
// }

/// <summary>
/// Base interface for expressions
/// </summary>
public abstract class Expression : ASTNode { }

/// <summary>
/// Represents literal values (strings, numbers, booleans, etc.)
/// </summary>
public class Literal : Expression
{
    public override NodeKind NodeKind => NodeKind.Literal;
    // public SourceLocation? Location { get; set; }

    public LiteralKind LiteralKind { get; set; }

    // [BK]: Boxing. Make it clean.
    public object? Value { get; set; }
}

/// <summary>
/// Types of literal values
/// </summary>
public enum LiteralKind
{
    String,
    Integer,
    Float,
    Boolean,
    Null,
    Duration,
    DateTime,
    Guid,
    Dynamic
}

/// <summary>
/// Type information for expressions - required for Expression Evaluation engine
/// Level 1 Support: Basic types for simple field projections and arithmetic
/// Level 2 Support (Future TODO): Complex types, arrays, nested objects
/// </summary>
public enum ExpressionType
{
    String,
    Integer,
    Float,
    Boolean,
    Duration,
    DateTime,
    Guid,
    Dynamic
}

/// <summary>
/// Binary operators used in expressions
/// </summary>
public enum BinaryOperatorKind
{
    Equal,           // ==
    NotEqual,        // !=
    LessThan,        // <
    LessThanOrEqual, // <=
    GreaterThan,     // >
    GreaterThanOrEqual, // >=
    Add,             // +
    Subtract,        // -
    Multiply,        // *
    Divide,          // /
    Modulo,          // %
    And,             // and
    Or               // or
}

/// <summary>
/// Identifiers (column/field names)
/// </summary>
public class Identifier : Expression
{
    public override NodeKind NodeKind => NodeKind.Identifier;
    // public SourceLocation? Location { get; set; }
    public required string Name { get; set; }
    public string? Namespace { get; set; } // For qualified names (e.g., span.name, trace.id in TraceQL)
}

/// <summary>
/// Binary expressions (comparisons, arithmetic, logical operations)
/// </summary>
public class BinaryExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.BinaryExpression;
    // public SourceLocation? Location { get; set; }
    public required BinaryOperatorKind Operator { get; set; }
    public required Expression Left { get; set; }
    public required Expression Right { get; set; }
}

/// <summary>
/// Unary expressions (NOT, negative, etc.)
/// </summary>
public class UnaryExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.UnaryExpression;
    // public SourceLocation? Location { get; set; }
    public required string Operator { get; set; } // !, -, etc.
    public required Expression Argument { get; set; }
}

/// <summary>
/// Function calls (count(), max(), etc.)
/// </summary>
public class CallExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.CallExpression;
    // public SourceLocation? Location { get; set; }
    public required Identifier Callee { get; set; }
    public List<Expression> Arguments { get; set; } = new List<Expression>();
}

/// <summary>
/// A grouped expression inside parentheses
/// </summary>
public class ParenthesizedExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.ParenthesizedExpression;
    // public SourceLocation? Location { get; set; }
    public required Expression Expression { get; set; }
}

/// <summary>
/// Special operators like IN, BETWEEN, etc.
/// </summary>
public enum SpecialOperatorKind
{
    In,             // IN
    NotIn,          // NOT IN
    Between,        // BETWEEN
    Contains,       // CONTAINS
    NotContains,    // NOT CONTAINS
}

/// <summary>
/// Special operators like IN, BETWEEN, etc.
/// </summary>
// [BK]: refactor this.
public class SpecialOperatorExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.SpecialOperatorExpression;
    // public SourceLocation? Location { get; set; }
    public required SpecialOperatorKind Operator { get; set; } // IN, NOT IN, BETWEEN, etc.
    public required Expression Left { get; set; }
    public List<Expression> Right { get; set; } = new List<Expression>(); // For multi-value operators like IN
}

/// <summary>
/// Wildcard expression (*)
/// </summary>
public class WildcardExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.WildcardExpression;
    // public SourceLocation? Location { get; set; }
}

/// <summary>
/// Path access expression (e.g., span.duration, trace.id)
/// Used primarily for TraceQL
/// </summary>
public class PathExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.PathExpression;
    // public SourceLocation? Location { get; set; }
    public required string Base { get; set; }
    public required string Path { get; set; }
}

/// <summary>
/// Builder for creating AST nodes
/// </summary>
public static class AstBuilder
{
    public static QueryNode CreateQuery(string? source = null)
    {
        return new QueryNode
        {
            Source = source,
            Operations = new List<OperationNode>()
        };
    }
    
    public static QueryNode CreateQueryWithOperations(List<OperationNode> operations, string? source = null)
    {
        return new QueryNode
        {
            Source = source,
            Operations = operations
        };
    }

    public static FilterNode CreateFilter(Expression expression, string? keyword = null/*, List<Parameter>? parameters = null*/)
    {
        return new FilterNode
        {
            TraceExpression = expression,
            Keyword = keyword,
            // Parameters = parameters
        };
    }
    
    /// <summary>
    /// Creates a FilterNode with both trace-level and span-level filtering
    /// </summary>
    public static FilterNode CreateCombinedFilter(
        Expression? traceExpression, 
        List<Expression>? spanExpressions = null,
        SpanFilterCombination spanCombination = SpanFilterCombination.Any,
        string? keyword = null)
    {
        var filterNode = new FilterNode
        {
            TraceExpression = traceExpression,
            Keyword = keyword
        };
        
        if (spanExpressions != null && spanExpressions.Count > 0)
        {
            filterNode.SpanFilter = new SpanFilter
            {
                Expressions = spanExpressions,
                Combination = spanCombination
            };
        }
        
        return filterNode;
    }
    
    /// <summary>
    /// Creates a FilterNode with only span-level filtering
    /// </summary>
    public static FilterNode CreateSpanFilter(
        List<Expression> spanExpressions,
        SpanFilterCombination combination = SpanFilterCombination.Any,
        string? keyword = null)
    {
        return new FilterNode
        {
            SpanFilter = new SpanFilter
            {
                Expressions = spanExpressions,
                Combination = combination
            },
            Keyword = keyword
        };
    }

    public static BinaryExpression CreateBinaryExpression(Expression left, BinaryOperatorKind op, Expression right)
    {
        return new BinaryExpression
        {
            Left = left,
            Operator = op,
            Right = right
        };
    }

    public static Literal CreateLiteral(object? value, LiteralKind valueType)
    {
        return new Literal
        {
            Value = value,
            LiteralKind = valueType
        };
    }

    public static Identifier CreateIdentifier(string name, string? ns = null)
    {
        return new Identifier
        {
            Name = name,
            Namespace = ns
        };
    }

    public static UnaryExpression CreateUnaryExpression(string op, Expression argument)
    {
        return new UnaryExpression
        {
            Operator = op,
            Argument = argument
        };
    }

    public static CallExpression CreateCallExpression(string functionName, List<Expression> arguments)
    {
        return new CallExpression
        {
            Callee = CreateIdentifier(functionName),
            Arguments = arguments
        };
    }

    public static SpecialOperatorExpression CreateSpecialOperatorExpression(Expression left, SpecialOperatorKind op, List<Expression> right)
    {
        return new SpecialOperatorExpression
        {
            Left = left,
            Operator = op,
            Right = right
        };
    }

    /// <summary>
    /// Creates a ProjectNode with specified projections
    /// Level 1 Support: Simple fields, aliases, basic arithmetic, simple functions
    /// </summary>
    public static ProjectNode CreateProject(List<ProjectionExpression> projections, string? keyword = null)
    {
        return new ProjectNode
        {
            Projections = projections,
            Keyword = keyword
        };
    }

    /// <summary>
    /// Creates a ProjectionExpression with optional alias and type
    /// Type should only be specified for calculated fields/function calls
    /// </summary>
    public static ProjectionExpression CreateProjection(Expression expression, string? alias = null, ExpressionType? resultType = null)
    {
        return new ProjectionExpression
        {
            Expression = expression,
            Alias = alias,
            ResultType = resultType
        };
    }

    /// <summary>
    /// Convenience method for simple field projection with alias
    /// </summary>
    public static ProjectionExpression CreateFieldProjection(string fieldName, string? alias = null, ExpressionType? resultType = null, string? ns = null)
    {
        return new ProjectionExpression
        {
            Expression = CreateIdentifier(fieldName, ns),
            Alias = alias,
            ResultType = resultType
        };
    }
}

/// <summary>
/// Sample usage examples of how to represent filters from both languages
/// </summary>
public static class Examples
{
    // KQL: | where a > 10 and b < 20
    public static QueryNode KqlWhereExample()
    {
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

        var andExpression = AstBuilder.CreateBinaryExpression(
            aGreaterThan10,
            BinaryOperatorKind.And,
            bLessThan20
        );

        var filterNode = AstBuilder.CreateFilter(andExpression, "where");
        
        var query = AstBuilder.CreateQuery("MyTable");
        query.Operations.Add(filterNode);
        
        return query;
    }

    // TraceQL: { span.duration > 100ms }
    public static QueryNode TraceQLFilterExample()
    {
        var spanDuration = AstBuilder.CreateIdentifier("duration", "span");

        var comparisonExpression = AstBuilder.CreateBinaryExpression(
            spanDuration,
            BinaryOperatorKind.GreaterThan,
            AstBuilder.CreateLiteral("100ms", LiteralKind.Duration)
        );

        var filterNode = AstBuilder.CreateFilter(comparisonExpression);
        
        var query = AstBuilder.CreateQuery();
        query.Operations.Add(filterNode);
        
        return query;
    }
    
    // Example with multiple operations in pipeline
    public static QueryNode QueryWithMultipleOperationsExample()
    {
        // First operation: filter
        var filterExpression = AstBuilder.CreateBinaryExpression(
            AstBuilder.CreateIdentifier("timestamp"),
            BinaryOperatorKind.GreaterThan,
            AstBuilder.CreateLiteral("2025-01-01", LiteralKind.DateTime)
        );
        var filterNode = AstBuilder.CreateFilter(filterExpression, "where");
        
        // Create query with operations
        var operations = new List<OperationNode> { filterNode };
        
        // Ready for more operations in the future
        // operations.Add(projectNode);
        // operations.Add(limitNode);
        
        return AstBuilder.CreateQueryWithOperations(operations, "Logs");
    }
    
    // Combined trace and span filtering example
    // Trace-level: trace.duration > 1s
    // Span-level: span.name = "db" OR span.name = "http"
    public static QueryNode CombinedFilteringExample()
    {
        // Trace-level filter
        var traceDuration = AstBuilder.CreateIdentifier("duration", "trace");
        var traceExpression = AstBuilder.CreateBinaryExpression(
            traceDuration,
            BinaryOperatorKind.GreaterThan,
            AstBuilder.CreateLiteral("1s", LiteralKind.Duration)
        );
        
        // Span-level filters
        var spanNameDb = AstBuilder.CreateBinaryExpression(
            AstBuilder.CreateIdentifier("name", "span"),
            BinaryOperatorKind.Equal,
            AstBuilder.CreateLiteral("db", LiteralKind.String)
        );
        
        var spanNameHttp = AstBuilder.CreateBinaryExpression(
            AstBuilder.CreateIdentifier("name", "span"),
            BinaryOperatorKind.Equal,
            AstBuilder.CreateLiteral("http", LiteralKind.String)
        );
        
        // Create a combined filter 
        var filterNode = AstBuilder.CreateCombinedFilter(
            traceExpression,
            new List<Expression> { spanNameDb, spanNameHttp },
            SpanFilterCombination.Any
        );
        
        // Create the full query
        var query = AstBuilder.CreateQuery();
        query.Operations.Add(filterNode);
        
        return query;
    }
    
    // Spans-only filtering example (no trace-level filtering)
    public static QueryNode SpansOnlyFilterExample()
    {
        // Span filters - find all spans with errors
        var spanStatusError = AstBuilder.CreateBinaryExpression(
            AstBuilder.CreateIdentifier("status", "span"),
            BinaryOperatorKind.Equal,
            AstBuilder.CreateLiteral("ERROR", LiteralKind.String)
        );
        
        var spanDurationHigh = AstBuilder.CreateBinaryExpression(
            AstBuilder.CreateIdentifier("duration", "span"),
            BinaryOperatorKind.GreaterThan,
            AstBuilder.CreateLiteral("200ms", LiteralKind.Duration)
        );
        
        // Create a spans-only filter that requires both conditions to be true
        var filterNode = AstBuilder.CreateSpanFilter(
            new List<Expression> { spanStatusError, spanDurationHigh },
            SpanFilterCombination.All
        );
        
        // Create the full query
        var query = AstBuilder.CreateQuery();
        query.Operations.Add(filterNode);
        
        return query;
    }
    
    // KQL: | project name, duration, status
    public static QueryNode KqlSimpleProjectExample()
    {
        var projections = new List<ProjectionExpression>
        {
            AstBuilder.CreateFieldProjection("name"),
            AstBuilder.CreateFieldProjection("duration"),
            AstBuilder.CreateFieldProjection("status")
        };
        
        var projectNode = AstBuilder.CreateProject(projections, "project");
        
        var query = AstBuilder.CreateQuery("MyTable");
        query.Operations.Add(projectNode);
        
        return query;
    }
    
    // KQL: | project service_name = name, duration_ms = duration / 1000, upper_name = toupper(name)
    public static QueryNode KqlProjectWithAliasesAndCalculationsExample()
    {
        var projections = new List<ProjectionExpression>
        {
            // Simple alias: service_name = name
            AstBuilder.CreateProjection(
                AstBuilder.CreateIdentifier("name"),
                alias: "service_name"
            ),
            
            // Calculated field: duration_ms = duration / 1000
            AstBuilder.CreateProjection(
                AstBuilder.CreateBinaryExpression(
                    AstBuilder.CreateIdentifier("duration"),
                    BinaryOperatorKind.Divide,
                    AstBuilder.CreateLiteral(1000, LiteralKind.Integer)
                ),
                alias: "duration_ms",
                resultType: ExpressionType.Float
            ),
            
            // Function call: upper_name = toupper(name)
            AstBuilder.CreateProjection(
                AstBuilder.CreateCallExpression("toupper", new List<Expression>
                {
                    AstBuilder.CreateIdentifier("name")
                }),
                alias: "upper_name",
                resultType: ExpressionType.String
            )
        };
        
        var projectNode = AstBuilder.CreateProject(projections, "project");
        
        var query = AstBuilder.CreateQuery("Logs");
        query.Operations.Add(projectNode);
        
        return query;
    }
    
    // TraceQL: select(span.name, span.duration, .service.name)
    public static QueryNode TraceQLSelectExample()
    {
        var projections = new List<ProjectionExpression>
        {
            AstBuilder.CreateFieldProjection("name", ns: "span"),
            AstBuilder.CreateFieldProjection("duration", ns: "span"),
            AstBuilder.CreateFieldProjection("name", ns: "service")
        };
        
        var selectNode = AstBuilder.CreateProject(projections, "select");
        
        var query = AstBuilder.CreateQuery();
        query.Operations.Add(selectNode);
        
        return query;
    }
    
    // KQL: MyTable | where timestamp > ago(1h) | project name, duration, category = case(duration > 1000, "slow", "fast")
    public static QueryNode QueryWithFilterAndProjectExample()
    {
        // Filter operation
        var filterExpression = AstBuilder.CreateBinaryExpression(
            AstBuilder.CreateIdentifier("timestamp"),
            BinaryOperatorKind.GreaterThan,
            AstBuilder.CreateCallExpression("ago", new List<Expression>
            {
                AstBuilder.CreateLiteral("1h", LiteralKind.Duration)
            })
        );
        var filterNode = AstBuilder.CreateFilter(filterExpression, "where");
        
        // Project operation
        var projections = new List<ProjectionExpression>
        {
            AstBuilder.CreateFieldProjection("name"),
            AstBuilder.CreateFieldProjection("duration"),
            
            // Level 2 TODO: case expressions - for now showing basic conditional concept
            AstBuilder.CreateProjection(
                AstBuilder.CreateCallExpression("case", new List<Expression>
                {
                    AstBuilder.CreateBinaryExpression(
                        AstBuilder.CreateIdentifier("duration"),
                        BinaryOperatorKind.GreaterThan,
                        AstBuilder.CreateLiteral(1000, LiteralKind.Integer)
                    ),
                    AstBuilder.CreateLiteral("slow", LiteralKind.String),
                    AstBuilder.CreateLiteral("fast", LiteralKind.String)
                }),
                alias: "category",
                resultType: ExpressionType.String
            )
        };
        var projectNode = AstBuilder.CreateProject(projections, "project");
        
        // Create query with both operations
        var operations = new List<OperationNode> { filterNode, projectNode };
        
        return AstBuilder.CreateQueryWithOperations(operations, "MyTable");
    }
}
