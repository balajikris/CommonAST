# CommonAST Design for Aggregations

## Overview

This document outlines the design of CommonAST nodes and patterns to support aggregation expressions, metrics operations, and group operations across TraceQL and KQL. The design is based on the semantic analysis in `aggregation-semantics.md` and aims to provide a unified, engine-agnostic representation while preserving cross-language translation capabilities.

## Design Principles

### 1. Engine-Agnostic Representation
- Focus on data processing operations, not engine-specific constructs
- Support execution on Arrow data via Expression Evaluation (EE) engine
- Exclude platform-specific optimizations and hints

### 2. Semantic Preservation
- Maintain semantic correctness across language translations
- Preserve functional equivalence between source and target languages
- Support round-trip translation where possible

### 3. Normalization Strategy
- Convert implicit grouping to explicit pipeline stages
- Provide consistent representation regardless of source syntax
- Enable uniform AST traversal and optimization

### 4. Extensibility
- Design for future language additions
- Support new aggregation types and metrics operations
- Maintain backward compatibility

## Current AST Analysis

### Existing Node Structure

The current CommonAST implementation provides a foundation but lacks specific aggregation support:

```csharp
// Current base classes
public abstract class OperationNode : ASTNode { }
public abstract class Expression : ASTNode { }

// Current operation types
public class FilterNode : OperationNode         // ✅ Filtering support
public class QueryNode : ASTNode               // ✅ Pipeline container

// Current expression types  
public class CallExpression : Expression       // ⚠️ Generic function calls
public class BinaryExpression : Expression     // ✅ Comparisons
public class Identifier : Expression           // ✅ Field references
```

### Limitations of Current Design

1. **No Dedicated Aggregation Nodes**: `CallExpression` is too generic
2. **Missing Group Operations**: No representation for `by` clauses
3. **No Metrics Distinction**: Cannot differentiate regular aggregates from metrics
4. **Limited Metadata**: No support for result naming or parameters
5. **No Multi-Operation Support**: Cannot handle KQL's multiple aggregations

## Proposed AST Extensions

### New NodeKind Enumerations

```csharp
public enum NodeKind
{
    // ... existing values ...
    
    // New aggregation-related node kinds
    AggregateOperation,      // Regular aggregate functions (sum, avg, etc.)
    MetricsOperation,        // Time-series metrics operations  
    GroupOperation,          // Grouping by fields
    ProjectOperation,        // Field selection/projection
    
    // Expression kinds for aggregation contexts
    AggregateExpression,     // Aggregate function references
    MetricsExpression,       // Metrics function references
    FieldReference,          // Field access with namespace support
}
```

### Core Aggregation Node Classes

#### 1. GroupOperation Node

```csharp
/// <summary>
/// Represents grouping operations (BY clauses) for aggregations
/// Normalizes both explicit and implicit grouping patterns
/// </summary>
public class GroupOperationNode : OperationNode
{
    public override NodeKind NodeKind => NodeKind.GroupOperation;
    
    /// <summary>
    /// List of fields to group by
    /// </summary>
    public List<FieldReference> GroupByFields { get; set; } = new List<FieldReference>();
    
    /// <summary>
    /// Optional binning expressions (for time/numeric grouping)
    /// </summary>
    public List<BinningExpression>? BinningExpressions { get; set; }
    
    /// <summary>
    /// Source language context for round-trip support
    /// </summary>
    public GroupingContext Context { get; set; } = GroupingContext.Explicit;
}

/// <summary>
/// Context information for grouping operation source
/// </summary>
public enum GroupingContext
{
    Explicit,    // Separate pipeline stage: {} | by (field) | aggregate
    Implicit,    // Embedded in operation: {} | aggregate by (field)  
    Embedded     // KQL-style: summarize ... by field
}

/// <summary>
/// Represents binning operations for time/numeric grouping
/// </summary>
public class BinningExpression : Expression
{
    public override NodeKind NodeKind => NodeKind.BinaryExpression; // Reuse existing
    
    public required Expression Field { get; set; }
    public required Expression BinSize { get; set; }
    public BinningType BinType { get; set; } = BinningType.Time;
}

public enum BinningType
{
    Time,       // Time-based binning (1h, 5m, etc.)
    Numeric,    // Numeric binning (ranges)
    Custom      // Custom binning logic
}
```

#### 2. AggregateOperation Node

```csharp
/// <summary>
/// Represents regular aggregate functions (sum, avg, min, max, count)
/// These require comparison operators in TraceQL and produce scalar results
/// </summary>
public class AggregateOperationNode : OperationNode
{
    public override NodeKind NodeKind => NodeKind.AggregateOperation;
    
    /// <summary>
    /// The aggregate function type
    /// </summary>
    public required AggregateFunction Function { get; set; }
    
    /// <summary>
    /// Field to aggregate (null for count())
    /// </summary>
    public FieldReference? Field { get; set; }
    
    /// <summary>
    /// Comparison operator for scalar filtering (TraceQL requirement)
    /// </summary>
    public ComparisonOperator? ComparisonOp { get; set; }
    
    /// <summary>
    /// Value to compare against
    /// </summary>
    public Expression? ComparisonValue { get; set; }
    
    /// <summary>
    /// Result name for multi-aggregation scenarios (KQL)
    /// </summary>
    public string? ResultName { get; set; }
    
    /// <summary>
    /// Associated grouping (normalized to explicit representation)
    /// </summary>
    public GroupOperationNode? GroupOperation { get; set; }
}

/// <summary>
/// Supported aggregate function types
/// </summary>
public enum AggregateFunction
{
    Count,
    Sum,
    Average,
    Minimum,
    Maximum,
    StandardDeviation,
    Variance,
    Percentile,
    DistinctCount,
    // KQL-specific functions
    MakeList,
    MakeSet,
    ArgumentMax,
    ArgumentMin,
    Any
}

/// <summary>
/// Comparison operators for aggregate filtering
/// </summary>
public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Match,        // =~ regex match
    NotMatch      // !~ regex not match
}
```

#### 3. MetricsOperation Node

```csharp
/// <summary>
/// Represents time-series metrics operations (sum_over_time, rate, etc.)
/// These produce time-series data and support both explicit and implicit grouping
/// </summary>
public class MetricsOperationNode : OperationNode
{
    public override NodeKind NodeKind => NodeKind.MetricsOperation;
    
    /// <summary>
    /// The metrics function type
    /// </summary>
    public required MetricsFunction Function { get; set; }
    
    /// <summary>
    /// Field to aggregate (null for rate(), count_over_time())
    /// </summary>
    public FieldReference? Field { get; set; }
    
    /// <summary>
    /// Parameters for complex metrics (e.g., quantile value)
    /// </summary>
    public List<Expression>? Parameters { get; set; }
    
    /// <summary>
    /// Associated grouping (normalized from implicit syntax)
    /// </summary>
    public GroupOperationNode? GroupOperation { get; set; }
    
    /// <summary>
    /// Result name for multi-metrics scenarios
    /// </summary>
    public string? ResultName { get; set; }
    
    /// <summary>
    /// Time window specification (if applicable)
    /// </summary>
    public TimeWindow? TimeWindow { get; set; }
}

/// <summary>
/// Supported metrics function types
/// </summary>
public enum MetricsFunction
{
    // Over-time aggregations
    SumOverTime,
    AverageOverTime,
    MinOverTime,
    MaxOverTime,
    CountOverTime,
    
    // Rate functions
    Rate,
    
    // Distribution functions
    HistogramOverTime,
    QuantileOverTime,
    
    // KQL time-series functions
    MakeSeries
}

/// <summary>
/// Time window specification for metrics
/// </summary>
public class TimeWindow : Expression
{
    public override NodeKind NodeKind => NodeKind.Literal;
    
    public required TimeSpan Duration { get; set; }
    public TimeWindowType Type { get; set; } = TimeWindowType.Sliding;
}

public enum TimeWindowType
{
    Sliding,    // Continuous sliding window
    Tumbling,   // Non-overlapping windows
    Session     // Session-based windows
}
```

#### 4. Enhanced FieldReference

```csharp
/// <summary>
/// Enhanced field reference with namespace support for cross-language compatibility
/// Replaces simple Identifier for aggregation contexts
/// </summary>
public class FieldReference : Expression
{
    public override NodeKind NodeKind => NodeKind.FieldReference;
    
    /// <summary>
    /// Field name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Namespace/scope (span, resource, trace, etc.)
    /// </summary>
    public string? Namespace { get; set; }
    
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
```

### Multi-Operation Support

#### Composite Operations for KQL

```csharp
/// <summary>
/// Composite operation to handle KQL's multiple aggregations in single summarize
/// Normalizes to multiple individual operations for cross-language compatibility
/// </summary>
public class CompositeAggregationNode : OperationNode
{
    public override NodeKind NodeKind => NodeKind.Query; // Reuse existing
    
    /// <summary>
    /// Multiple aggregation operations with shared grouping
    /// </summary>
    public List<AggregateOperationNode> Aggregations { get; set; } = new List<AggregateOperationNode>();
    
    /// <summary>
    /// Multiple metrics operations with shared grouping
    /// </summary>
    public List<MetricsOperationNode> Metrics { get; set; } = new List<MetricsOperationNode>();
    
    /// <summary>
    /// Shared grouping for all operations
    /// </summary>
    public GroupOperationNode? SharedGrouping { get; set; }
    
    /// <summary>
    /// Source language context
    /// </summary>
    public string SourceLanguage { get; set; } = "KQL";
}
```

## Builder Pattern Extensions

### Enhanced AstBuilder Methods

```csharp
public static class AstBuilder
{
    // ... existing methods ...
    
    #region Group Operations
    
    /// <summary>
    /// Creates a group operation with field list
    /// </summary>
    public static GroupOperationNode CreateGroupOperation(
        List<FieldReference> groupByFields,
        GroupingContext context = GroupingContext.Explicit,
        List<BinningExpression>? binning = null)
    {
        return new GroupOperationNode
        {
            GroupByFields = groupByFields,
            Context = context,
            BinningExpressions = binning
        };
    }
    
    /// <summary>
    /// Creates a group operation from field names
    /// </summary>
    public static GroupOperationNode CreateGroupOperation(
        params string[] fieldNames)
    {
        var fields = fieldNames.Select(name => CreateFieldReference(name)).ToList();
        return CreateGroupOperation(fields);
    }
    
    #endregion
    
    #region Aggregate Operations
    
    /// <summary>
    /// Creates an aggregate operation with optional comparison
    /// </summary>
    public static AggregateOperationNode CreateAggregateOperation(
        AggregateFunction function,
        FieldReference? field = null,
        ComparisonOperator? comparisonOp = null,
        Expression? comparisonValue = null,
        string? resultName = null)
    {
        return new AggregateOperationNode
        {
            Function = function,
            Field = field,
            ComparisonOp = comparisonOp,
            ComparisonValue = comparisonValue,
            ResultName = resultName
        };
    }
    
    /// <summary>
    /// Creates a count aggregate operation
    /// </summary>
    public static AggregateOperationNode CreateCountOperation(
        ComparisonOperator? comparisonOp = null,
        Expression? comparisonValue = null)
    {
        return CreateAggregateOperation(
            AggregateFunction.Count,
            comparisonOp: comparisonOp,
            comparisonValue: comparisonValue);
    }
    
    /// <summary>
    /// Creates a sum aggregate operation
    /// </summary>
    public static AggregateOperationNode CreateSumOperation(
        FieldReference field,
        ComparisonOperator? comparisonOp = null,
        Expression? comparisonValue = null)
    {
        return CreateAggregateOperation(
            AggregateFunction.Sum,
            field,
            comparisonOp,
            comparisonValue);
    }
    
    #endregion
    
    #region Metrics Operations
    
    /// <summary>
    /// Creates a metrics operation 
    /// </summary>
    public static MetricsOperationNode CreateMetricsOperation(
        MetricsFunction function,
        FieldReference? field = null,
        List<Expression>? parameters = null,
        GroupOperationNode? groupOperation = null,
        string? resultName = null)
    {
        return new MetricsOperationNode
        {
            Function = function,
            Field = field,
            Parameters = parameters,
            GroupOperation = groupOperation,
            ResultName = resultName
        };
    }
    
    /// <summary>
    /// Creates a sum_over_time metrics operation
    /// </summary>
    public static MetricsOperationNode CreateSumOverTimeOperation(
        FieldReference field,
        GroupOperationNode? groupOperation = null)
    {
        return CreateMetricsOperation(
            MetricsFunction.SumOverTime,
            field,
            groupOperation: groupOperation);
    }
    
    #endregion
    
    #region Field References
    
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
    /// Creates a field reference from TraceQL-style path (span.latency)
    /// </summary>
    public static FieldReference CreateFieldReferenceFromPath(string path)
    {
        var parts = path.Split('.');
        if (parts.Length == 1)
        {
            return CreateFieldReference(parts[0]);
        }
        else if (parts.Length == 2)
        {
            return CreateFieldReference(parts[1], parts[0]);
        }
        else
        {
            // Handle complex paths: parent.span.field
            var nameSpace = string.Join(".", parts.Take(parts.Length - 1));
            return CreateFieldReference(parts.Last(), nameSpace);
        }
    }
    
    #endregion
    
    #region Composite Operations
    
    /// <summary>
    /// Creates a composite aggregation for KQL multi-aggregate scenarios
    /// </summary>
    public static CompositeAggregationNode CreateCompositeAggregation(
        List<AggregateOperationNode>? aggregations = null,
        List<MetricsOperationNode>? metrics = null,
        GroupOperationNode? sharedGrouping = null)
    {
        return new CompositeAggregationNode
        {
            Aggregations = aggregations ?? new List<AggregateOperationNode>(),
            Metrics = metrics ?? new List<MetricsOperationNode>(),
            SharedGrouping = sharedGrouping
        };
    }
    
    #endregion
}
```

## Normalization Patterns

### TraceQL Implicit → Explicit Grouping

```csharp
/// <summary>
/// Normalizes TraceQL implicit grouping to explicit pipeline stages
/// </summary>
public class TraceQLNormalizer
{
    /// <summary>
    /// Converts: {} | sum_over_time(.latency) by (resource.service)
    /// To: {} | by (resource.service) | sum_over_time(.latency)
    /// </summary>
    public static QueryNode NormalizeImplicitGrouping(MetricsOperationNode metricsOp)
    {
        var query = AstBuilder.CreateQuery();
        
        // If metrics operation has embedded grouping, extract it
        if (metricsOp.GroupOperation != null)
        {
            // Add explicit group operation
            query.Operations.Add(metricsOp.GroupOperation);
            
            // Remove grouping from metrics operation
            var normalizedMetrics = AstBuilder.CreateMetricsOperation(
                metricsOp.Function,
                metricsOp.Field,
                metricsOp.Parameters,
                groupOperation: null, // Remove embedded grouping
                metricsOp.ResultName);
            
            query.Operations.Add(normalizedMetrics);
        }
        else
        {
            // No normalization needed
            query.Operations.Add(metricsOp);
        }
        
        return query;
    }
}
```

### KQL Multi-Aggregation → Pipeline Decomposition

```csharp
/// <summary>
/// Normalizes KQL multi-aggregation to individual pipeline operations
/// </summary>
public class KQLNormalizer
{
    /// <summary>
    /// Converts: | summarize count(), avg(Duration) by ResourceGroup
    /// To: [GroupOperation, AggregateOperation(count), AggregateOperation(avg)]
    /// </summary>
    public static QueryNode NormalizeCompositeAggregation(CompositeAggregationNode composite)
    {
        var query = AstBuilder.CreateQuery();
        
        // Add shared grouping if present
        if (composite.SharedGrouping != null)
        {
            query.Operations.Add(composite.SharedGrouping);
        }
        
        // Add individual aggregate operations
        foreach (var agg in composite.Aggregations)
        {
            // Remove embedded grouping since it's now explicit
            agg.GroupOperation = null;
            query.Operations.Add(agg);
        }
        
        // Add individual metrics operations
        foreach (var metric in composite.Metrics)
        {
            // Remove embedded grouping since it's now explicit
            metric.GroupOperation = null;
            query.Operations.Add(metric);
        }
        
        return query;
    }
}
```

## Usage Examples

### TraceQL Query Representations

#### Example 1: Explicit Grouping
```traceql
{} | by (resource.service) | sum(.latency) > 100
```

**CommonAST Representation:**
```csharp
var query = AstBuilder.CreateQuery();

// Group operation
var groupOp = AstBuilder.CreateGroupOperation("resource.service");
query.Operations.Add(groupOp);

// Aggregate operation  
var aggregateOp = AstBuilder.CreateSumOperation(
    AstBuilder.CreateFieldReferenceFromPath(".latency"),
    ComparisonOperator.GreaterThan,
    AstBuilder.CreateLiteral(100, LiteralKind.Integer));
query.Operations.Add(aggregateOp);
```

#### Example 2: Implicit Grouping (Normalized)
```traceql
{} | sum_over_time(.latency) by (resource.service)
```

**CommonAST Representation (After Normalization):**
```csharp
var query = AstBuilder.CreateQuery();

// Normalized to explicit grouping
var groupOp = AstBuilder.CreateGroupOperation("resource.service");
query.Operations.Add(groupOp);

// Metrics operation (grouping removed)
var metricsOp = AstBuilder.CreateSumOverTimeOperation(
    AstBuilder.CreateFieldReferenceFromPath(".latency"));
query.Operations.Add(metricsOp);
```

### KQL Query Representations

#### Example 3: KQL Summarize
```kql
| summarize TotalCount = count(), AvgDuration = avg(Duration) by ResourceGroup
```

**CommonAST Representation (After Normalization):**
```csharp
var query = AstBuilder.CreateQuery();

// Shared grouping
var groupOp = AstBuilder.CreateGroupOperation("ResourceGroup");
query.Operations.Add(groupOp);

// First aggregation
var countOp = AstBuilder.CreateCountOperation();
countOp.ResultName = "TotalCount";
query.Operations.Add(countOp);

// Second aggregation
var avgOp = AstBuilder.CreateAggregateOperation(
    AggregateFunction.Average,
    AstBuilder.CreateFieldReference("Duration"));
avgOp.ResultName = "AvgDuration";
query.Operations.Add(avgOp);
```

## Cross-Language Translation

### TraceQL ↔ KQL Mapping

#### Translation Rules

| TraceQL Pattern | KQL Equivalent | CommonAST Operations |
|----------------|----------------|---------------------|
| `{} \| by (field) \| count() > 0` | `\| summarize count() by field` | `GroupOperation + AggregateOperation` |
| `{} \| sum_over_time(field) by (group)` | `\| summarize sum(field) by group, bin(time, 1h)` | `GroupOperation + MetricsOperation` |
| `{} \| by (field) \| sum(x) > 10` | `\| where field \| summarize sum(x) \| where sum_x > 10` | `FilterNode + GroupOperation + AggregateOperation` |

#### Translation Challenges

1. **Field Namespace Mapping**:
   - TraceQL: `span.latency` → KQL: `SpanLatency`
   - Requires field mapping tables

2. **Comparison Semantics**:
   - TraceQL: Inline comparisons in pipeline
   - KQL: Separate `where` clauses after aggregation

3. **Time Series Handling**:
   - TraceQL: Implicit time series from `*_over_time` functions
   - KQL: Explicit `bin(TimeGenerated, interval)` grouping

4. **Result Naming**:
   - TraceQL: No explicit naming
   - KQL: Required result naming for aggregations

## Validation and Type Safety

### Field Validation

```csharp
/// <summary>
/// Validates field references and data types for aggregation operations
/// </summary>
public class AggregationValidator
{
    /// <summary>
    /// Validates that field supports the requested aggregation function
    /// </summary>
    public static ValidationResult ValidateAggregation(
        AggregateFunction function, 
        FieldReference? field)
    {
        if (function == AggregateFunction.Count && field != null)
        {
            return ValidationResult.Warning("Count function doesn't require field parameter");
        }
        
        if (function != AggregateFunction.Count && field == null)
        {
            return ValidationResult.Error("Aggregate function requires field parameter");
        }
        
        if (field?.DataType != null)
        {
            return ValidateDataTypeCompatibility(function, field.DataType.Value);
        }
        
        return ValidationResult.Success();
    }
    
    private static ValidationResult ValidateDataTypeCompatibility(
        AggregateFunction function, 
        DataType dataType)
    {
        var numericFunctions = new[] { 
            AggregateFunction.Sum, 
            AggregateFunction.Average,
            AggregateFunction.Minimum,
            AggregateFunction.Maximum 
        };
        
        if (numericFunctions.Contains(function) && 
            !IsNumericType(dataType))
        {
            return ValidationResult.Error(
                $"Function {function} requires numeric field, got {dataType}");
        }
        
        return ValidationResult.Success();
    }
    
    private static bool IsNumericType(DataType dataType)
    {
        return dataType == DataType.Integer || 
               dataType == DataType.Float || 
               dataType == DataType.Duration;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public ValidationLevel Level { get; set; }
    
    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Error(string message) => new() { IsValid = false, Message = message, Level = ValidationLevel.Error };
    public static ValidationResult Warning(string message) => new() { IsValid = true, Message = message, Level = ValidationLevel.Warning };
}

public enum ValidationLevel
{
    Success,
    Warning,
    Error
}
```

## Future Considerations

### Extensibility Points

1. **New Languages**: Framework supports additional query languages
2. **Custom Functions**: Extensible function registry
3. **Advanced Metrics**: Support for complex time-series operations
4. **Optimization Hints**: Optional optimization metadata

### Performance Optimizations

1. **Function Inlining**: Inline simple aggregations during translation
2. **Group Coalescing**: Combine consecutive group operations
3. **Field Pruning**: Remove unused fields from aggregations
4. **Predicate Pushdown**: Move filters before aggregations when possible

### Integration Points

1. **Expression Evaluation Engine**: Direct AST execution on Arrow data
2. **Query Optimizers**: AST transformation for performance
3. **Cross-Language IDEs**: AST-based syntax highlighting and completion
4. **Documentation Generation**: Auto-generate docs from AST patterns

## Conclusion

This CommonAST design provides:

1. **Unified Representation**: Single AST structure for all aggregation patterns
2. **Cross-Language Support**: Seamless TraceQL ↔ KQL translation
3. **Semantic Preservation**: Maintains functional correctness across translations
4. **Extensibility**: Framework for future enhancements
5. **Type Safety**: Validation and error checking capabilities

The design normalizes different syntactic approaches to equivalent semantic operations, enabling robust cross-language analysis and translation while maintaining the semantic richness of each source language.
