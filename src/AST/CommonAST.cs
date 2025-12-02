namespace CommonAST;
using System.Collections.Generic;

/// <summary>
/// Enum representing the type of node in the AST
/// </summary>
public enum NodeKind
{
    Query,
    Filter,
    Literal,
    Identifier,
    BinaryExpression,
    UnaryExpression,
    CallExpression,
    ParenthesizedExpression,
    SpecialOperatorExpression,
    WildcardExpression,
    PathExpression,
    
    // Phase 1: Aggregation support
    FieldReference,
    NamedExpression,
    CompositeAggregation
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

#region Phase 1: Aggregation Support

/// <summary>
/// Field categorization for different data sources
/// </summary>
public enum FieldType
{
    Intrinsic,    // Built-in fields (duration, name, etc.)
    Attribute,    // Custom attributes
    Metadata,     // System metadata
    Computed      // Derived/computed fields
}

/// <summary>
/// Data types for field validation
/// </summary>
public enum DataType
{
    String,
    Integer,
    Float,
    Boolean,
    Duration,
    DateTime,
    Array,
    Object,
    Unknown
}

/// <summary>
/// Phase 1 aggregate function types (implementation priority)
/// </summary>
public enum AggregateFunction 
{
    // Phase 1: Core 5 functions
    Count,
    Sum, 
    Average,
    Minimum,
    Maximum
    
    // Phase 2: Additional functions (design only, not implemented)
    // StandardDeviation, Variance, Percentile, DistinctCount,
    // MakeList, MakeSet, ArgumentMax, ArgumentMin, Any
}

/// <summary>
/// Enhanced field reference with namespace support for cross-language compatibility
/// Inherits from Identifier and adds aggregation-specific metadata
/// </summary>
public class FieldReference : Identifier
{
    public override NodeKind NodeKind => NodeKind.FieldReference;
    
    /// <summary>
    /// Field type for validation and optimization
    /// </summary>
    public FieldType FieldType { get; set; } = FieldType.Attribute;
    
    /// <summary>
    /// Data type of the field
    /// </summary>
    public DataType? DataType { get; set; }
    
    /// <summary>
    /// Whether this field is required for the operation
    /// </summary>
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Represents a named expression used in project, extend, summarize and other operators
/// Supports both single names and multiple names: name = expr, (name1, name2) = expr
/// </summary>
public class NamedExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.NamedExpression;
    
    /// <summary>
    /// Single result name (most common case)
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Multiple result names for tuple destructuring: (name1, name2) = expr
    /// </summary>
    public List<string>? Names { get; set; }
    
    /// <summary>
    /// The expression being named
    /// </summary>
    public required Expression Expression { get; set; }
    
    /// <summary>
    /// True if this expression has explicit naming
    /// </summary>
    public bool IsNamed => Name != null || (Names != null && Names.Count > 0);
}

/// <summary>
/// Individual aggregate operation within CompositeAggregationNode
/// </summary>
public class AggregateOperationNode
{
    /// <summary>Phase 1: Only these 5 functions</summary>
    public required AggregateFunction Function { get; set; }
    
    /// <summary>Field to aggregate (null for count())</summary>
    public FieldReference? Field { get; set; }
    
    /// <summary>KQL-specific: Result column name</summary>
    public string? ResultName { get; set; }
    
    /// <summary>Source expression for complex aggregations</summary>
    public Expression? SourceExpression { get; set; }
}

/// <summary>
/// Unified node for aggregation operations from both KQL and TraceQL
/// Handles grouping + aggregations in a single operation
/// Supports group-only, aggregate-only, and mixed operations
/// </summary>
public class CompositeAggregationNode : OperationNode
{
    public override NodeKind NodeKind => NodeKind.CompositeAggregation;
    
    /// <summary>
    /// List of fields to group by (optional)
    /// Empty list = no grouping
    /// </summary>
    public List<FieldReference> GroupByFields { get; set; } = new List<FieldReference>();
    
    /// <summary>
    /// List of aggregation operations (optional)
    /// Empty list = group-only operation
    /// </summary>
    public List<AggregateOperationNode> Aggregations { get; set; } = new List<AggregateOperationNode>();
    
    /// <summary>
    /// Source language context for translation
    /// </summary>
    public string SourceLanguage { get; set; } = "Unknown";
    
    /// <summary>
    /// Validation: Must have either grouping OR aggregations (or both)
    /// </summary>
    public bool IsValid => GroupByFields.Count > 0 || Aggregations.Count > 0;
    
    /// <summary>
    /// Operation classification
    /// </summary>
    public bool IsGroupOnly => Aggregations.Count == 0 && GroupByFields.Count > 0;
    public bool IsAggregateOnly => Aggregations.Count > 0 && GroupByFields.Count == 0;
    public bool IsMixed => Aggregations.Count > 0 && GroupByFields.Count > 0;
}

#endregion

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

    #region Phase 1: Aggregation Builder Methods

    /// <summary>
    /// Creates a field reference with namespace support
    /// </summary>
    public static FieldReference CreateFieldReference(
        string name,
        string? nameSpace = null,
        FieldType fieldType = FieldType.Attribute,
        DataType? dataType = null)
    {
        return new FieldReference
        {
            Name = name,
            Namespace = nameSpace,
            FieldType = fieldType,
            DataType = dataType
        };
    }

    /// <summary>
    /// Creates a named expression
    /// </summary>
    public static NamedExpression CreateNamedExpression(
        Expression expression,
        string? name = null,
        List<string>? names = null)
    {
        return new NamedExpression
        {
            Expression = expression,
            Name = name,
            Names = names
        };
    }

    /// <summary>
    /// Creates an aggregate operation node
    /// </summary>
    public static AggregateOperationNode CreateAggregateOperation(
        AggregateFunction function,
        FieldReference? field = null,
        string? resultName = null,
        Expression? sourceExpression = null)
    {
        return new AggregateOperationNode
        {
            Function = function,
            Field = field,
            ResultName = resultName,
            SourceExpression = sourceExpression
        };
    }

    /// <summary>
    /// Creates a composite aggregation node (unified for both KQL and TraceQL)
    /// </summary>
    public static CompositeAggregationNode CreateCompositeAggregation(
        List<FieldReference>? groupByFields = null,
        List<AggregateOperationNode>? aggregations = null,
        string sourceLanguage = "Unknown")
    {
        return new CompositeAggregationNode
        {
            GroupByFields = groupByFields ?? new List<FieldReference>(),
            Aggregations = aggregations ?? new List<AggregateOperationNode>(),
            SourceLanguage = sourceLanguage
        };
    }

    /// <summary>
    /// Creates a group-only composite aggregation (KQL: summarize by fields)
    /// </summary>
    public static CompositeAggregationNode CreateGroupOnlyAggregation(
        List<FieldReference> groupByFields,
        string sourceLanguage = "KQL")
    {
        return new CompositeAggregationNode
        {
            GroupByFields = groupByFields,
            Aggregations = new List<AggregateOperationNode>(), // Empty = group-only
            SourceLanguage = sourceLanguage
        };
    }

    /// <summary>
    /// Creates an aggregate-only composite aggregation (KQL: summarize aggregates)
    /// </summary>
    public static CompositeAggregationNode CreateAggregateOnlyAggregation(
        List<AggregateOperationNode> aggregations,
        string sourceLanguage = "KQL")
    {
        return new CompositeAggregationNode
        {
            GroupByFields = new List<FieldReference>(), // Empty = no grouping
            Aggregations = aggregations,
            SourceLanguage = sourceLanguage
        };
    }

    /// <summary>
    /// Creates a count aggregate operation
    /// </summary>
    public static AggregateOperationNode CreateCountOperation(string? resultName = null)
    {
        return CreateAggregateOperation(AggregateFunction.Count, null, resultName);
    }

    /// <summary>
    /// Creates a sum aggregate operation
    /// </summary>
    public static AggregateOperationNode CreateSumOperation(
        FieldReference field,
        string? resultName = null)
    {
        return CreateAggregateOperation(AggregateFunction.Sum, field, resultName);
    }

    /// <summary>
    /// Creates an average aggregate operation
    /// </summary>
    public static AggregateOperationNode CreateAverageOperation(
        FieldReference field,
        string? resultName = null)
    {
        return CreateAggregateOperation(AggregateFunction.Average, field, resultName);
    }

    /// <summary>
    /// Creates a minimum aggregate operation
    /// </summary>
    public static AggregateOperationNode CreateMinimumOperation(
        FieldReference field,
        string? resultName = null)
    {
        return CreateAggregateOperation(AggregateFunction.Minimum, field, resultName);
    }

    /// <summary>
    /// Creates a maximum aggregate operation
    /// </summary>
    public static AggregateOperationNode CreateMaximumOperation(
        FieldReference field,
        string? resultName = null)
    {
        return CreateAggregateOperation(AggregateFunction.Maximum, field, resultName);
    }

    #endregion
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
}
