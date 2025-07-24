# Aggregation Semantics & Nuances

## Overview

This document analyzes the semantics and syntax nuances of aggregation expressions, metrics operations, and group operations across query languages supported by CommonAST. Understanding these differences is crucial for designing a unified AST representation that preserves semantic correctness while enabling cross-language translation.

## TraceQL Aggregation Semantics

### Function Categories

TraceQL supports two distinct categories of aggregation-like operations, each with different syntax rules and grouping support:

1. **Regular Aggregate Functions**: `sum()`, `avg()`, `min()`, `max()`, `count()`
2. **Metrics Operations**: `sum_over_time()`, `avg_over_time()`, `rate()`, `count_over_time()`, etc.

### Group Operation Support Matrix

Based on empirical testing with the TraceQL parser, here's the definitive support matrix:

| Function Category | Function | Explicit Grouping | Implicit Grouping | Parse Result | AST Structure |
|------------------|----------|-------------------|-------------------|--------------|---------------|
| **Regular Aggregates** | `sum(field)` | ✅ `{} \| by (resource.service) \| sum(.latency) > 100` | ❌ `{} \| sum(.latency) > 100 by (resource.service)` | **Parse Error** | Separate `GroupOperation` + `ScalarFilter` |
| | `avg(field)` | ✅ `{} \| by (resource.service) \| avg(.latency) > 100` | ❌ `{} \| avg(.latency) > 100 by (resource.service)` | **Parse Error** | Separate `GroupOperation` + `ScalarFilter` |
| | `min(field)` | ✅ `{} \| by (resource.service) \| min(.latency) > 100` | ❌ `{} \| min(.latency) > 100 by (resource.service)` | **Parse Error** | Separate `GroupOperation` + `ScalarFilter` |
| | `max(field)` | ✅ `{} \| by (resource.service) \| max(.latency) > 100` | ❌ `{} \| max(.latency) > 100 by (resource.service)` | **Parse Error** | Separate `GroupOperation` + `ScalarFilter` |
| | `count()` | ✅ `{} \| by (resource.service) \| count() > 100` | ❌ `{} \| count() > 100 by (resource.service)` | **Parse Error** | Separate `GroupOperation` + `ScalarFilter` |
| **Metrics Operations** | `sum_over_time(field)` | ✅ `{} \| by (resource.service) \| sum_over_time(.latency)` | ✅ `{} \| sum_over_time(.latency) by (resource.service)` | **Both Valid** | Different AST structures |
| | `avg_over_time(field)` | ✅ `{} \| by (resource.service) \| avg_over_time(.latency)` | ✅ `{} \| avg_over_time(.latency) by (resource.service)` | **Both Valid** | Different AST structures |
| | `min_over_time(field)` | ✅ `{} \| by (resource.service) \| min_over_time(.latency)` | ✅ `{} \| min_over_time(.latency) by (resource.service)` | **Both Valid** | Different AST structures |
| | `max_over_time(field)` | ✅ `{} \| by (resource.service) \| max_over_time(.latency)` | ✅ `{} \| max_over_time(.latency) by (resource.service)` | **Both Valid** | Different AST structures |
| | `count_over_time()` | ✅ `{} \| by (resource.service) \| count_over_time()` | ✅ `{} \| count_over_time() by (resource.service)` | **Both Valid** | Different AST structures |
| | `rate()` | ✅ `{} \| by (resource.service) \| rate()` | ✅ `{} \| rate() by (resource.service)` | **Both Valid** | Different AST structures |
| | `histogram_over_time(field)` | ✅ `{} \| by (resource.service) \| histogram_over_time(.latency)` | ✅ `{} \| histogram_over_time(.latency) by (resource.service)` | **Both Valid** | Different AST structures |
| | `quantile_over_time(q, field)` | ✅ `{} \| by (resource.service) \| quantile_over_time(0.95, .latency)` | ✅ `{} \| quantile_over_time(0.95, .latency) by (resource.service)` | **Both Valid** | Different AST structures |

### Key Grammar Insights

#### Regular Aggregates (Limited Grouping Support)
- **Grammar Rule**: `ScalarFilter` → `ScalarExpression ComparisonOp ScalarExpression`
- **Aggregate Rule**: `Aggregate` → `AggregateExpression "(" FieldExpression ")"`
- **Limitation**: No `by` clause support in `ScalarFilter` grammar
- **Required Pattern**: Explicit grouping as separate pipeline stage

```traceql
# ✅ Valid: Explicit grouping
{} | by (resource.service) | sum(.latency) > 100

# ❌ Invalid: Implicit grouping fails to parse  
{} | sum(.latency) > 100 by (resource.service)
```

#### Metrics Operations (Full Grouping Support)
- **Grammar Rule**: `MetricsOperation` → `MetricsOverTime` or `MetricsOperationBasic`
- **Grouping Rule**: `MetricsOverTime` → `MetricsOverTimeType "(" MetricsExpression ")" GroupOperation?`
- **Flexibility**: Optional embedded `GroupOperation` clause
- **Dual Support**: Both explicit and implicit grouping patterns work

```traceql
# ✅ Valid: Explicit grouping
{} | by (resource.service) | sum_over_time(.latency)

# ✅ Valid: Implicit grouping  
{} | sum_over_time(.latency) by (resource.service)
```

### Parse Tree Analysis

Our empirical testing revealed distinct AST structures for equivalent queries:

#### Explicit Grouping Pattern
```
SpansetPipelineExpression
├─ SpansetPipelineExpression: "{}"
├─ Pipe: "|"
├─ SpansetPipelineExpression: "by (resource.service)"
│  └─ GroupOperation: "by (resource.service)"
├─ Pipe: "|"  
└─ SpansetPipelineExpression: "sum_over_time(.latency)"
   └─ MetricsOperation: "sum_over_time(.latency)"
```

#### Implicit Grouping Pattern  
```
SpansetPipelineExpression
├─ SpansetPipelineExpression: "{}"
├─ Pipe: "|"
└─ SpansetPipelineExpression: "sum_over_time(.latency) by (resource.service)"
   └─ MetricsOperation: "sum_over_time(.latency) by (resource.service)"
      ├─ MetricsOverTime: "sum_over_time(.latency)"
      └─ GroupOperation: "by (resource.service)"  [EMBEDDED]
```

### Semantic Equivalence

Despite different AST structures, both patterns are **functionally equivalent**:

1. **Same Data Processing**: Both group spans by the specified field(s)
2. **Same Result Set**: Both produce identical grouped time-series data  
3. **Same Execution Semantics**: The grouping happens before aggregation in both cases

However, the structural differences have implications for:
- **Query Optimization**: Different execution plan opportunities
- **Cross-Language Translation**: Need to normalize representation
- **AST Processing**: Different traversal patterns required

### Field Type Support

TraceQL aggregations support various field types:

#### Intrinsic Fields
```traceql
{} | sum(duration)           # Built-in span duration
{} | sum(trace:duration)     # Built-in trace duration  
{} | count()                 # No field required
```

#### Attribute Fields
```traceql
{} | sum(span.latency)       # Span attribute
{} | sum(resource.cpu)       # Resource attribute
{} | sum(.custom_metric)     # Any attribute (no namespace)
```

### Error Patterns

Common syntax errors and their causes:

#### Misplaced `by` Clause
```traceql
# ❌ Error: "Unexpected identifier: 'by'"
{} | sum(.latency) > 100 by (resource.service)

# ✅ Fix: Move grouping before aggregation
{} | by (resource.service) | sum(.latency) > 100
```

#### Missing Comparison Operator
```traceql
# ❌ Error: "Unexpected end of input" 
{} | by (resource.service) | sum(.latency)

# ✅ Fix: Add comparison for scalar filter
{} | by (resource.service) | sum(.latency) > 100
```

### Best Practices

1. **Use Explicit Grouping**: For consistency across all aggregation types
2. **Metrics vs Aggregates**: Understand the semantic differences  
3. **Field Validation**: Ensure field exists and has appropriate type
4. **Comparison Requirements**: Regular aggregates require comparison operators

## KQL Aggregation Semantics

### Overview

KQL (Kusto Query Language) provides rich aggregation capabilities through the `summarize` operator and various aggregate functions. Unlike TraceQL's pipeline-based approach, KQL uses a more declarative syntax.

### Aggregate Functions

Based on the KQL grammar analysis, KQL supports:

#### Built-in Aggregates (from `summarizeOperator` grammar)
- Standard functions: `count()`, `sum()`, `avg()`, `min()`, `max()`
- Statistical functions: `stdev()`, `variance()`, `percentile()`
- Collection functions: `make_list()`, `make_set()`, `dcount()`
- Advanced functions: `arg_max()`, `arg_min()`, `any()`

#### Time-Based Aggregations
- Window functions through `bin()` expressions
- Time series operations with `make-series` operator

### Group Operations

KQL grouping is handled through the `by` clause in `summarize`:

```kql
// Basic grouping
| summarize count() by ResourceGroup

// Multiple grouping fields  
| summarize avg(Duration) by ResourceGroup, Operation

// Time-based grouping
| summarize count() by bin(TimeGenerated, 1h), ResourceGroup
```

### Syntax Patterns

#### Standard Summarize Pattern
```kql
| summarize 
    TotalCount = count(),
    AvgDuration = avg(Duration)
  by ResourceGroup, bin(TimeGenerated, 1h)
```

#### Key Differences from TraceQL
1. **Declarative Style**: All aggregations and grouping in single operator
2. **Named Results**: Explicit naming of aggregated columns
3. **No Pipeline Grouping**: Grouping always embedded in `summarize`
4. **Rich Binning**: Built-in support for time/numeric binning

### Grammar Analysis

From `Kql.g4`, the `summarizeOperator` rule shows:

```antlr
summarizeOperator:
    SUMMARIZE (Parameters+=strictQueryOperatorParameter)* 
    (Expressions+=namedExpression (',' Expressions+=namedExpression)*)? 
    (ByClause=summarizeOperatorByClause)?;

summarizeOperatorByClause:
    BY Expressions+=namedExpression (',' Expressions+=namedExpression) 
    (BinClause=summarizeOperatorLegacyBinClause)?;
```

This reveals:
- **Optional Grouping**: `by` clause is optional
- **Multiple Aggregations**: Multiple expressions in single operator
- **Named Expressions**: All results can be named
- **Legacy Binning**: Historical `bin=` syntax support

### Cross-Language Mapping Challenges

Mapping between KQL and TraceQL aggregations presents several challenges:

#### Structural Differences
| Aspect | KQL | TraceQL |
|--------|-----|---------|
| **Grouping** | Embedded in `summarize` | Separate pipeline stage |
| **Multiple Aggregations** | Single operator | Multiple pipeline stages |
| **Result Naming** | Explicit naming required | No naming concept |
| **Binning** | Built-in `bin()` function | No built-in binning |

#### Semantic Mapping
- **KQL `summarize count() by field`** → **TraceQL `{} \| by (field) \| count() > 0`**
- **KQL multiple aggregates** → **TraceQL multiple pipeline stages**
- **KQL named results** → **TraceQL requires additional AST metadata**

### Future Considerations

For CommonAST design, KQL aggregations require:

1. **Multi-Aggregation Support**: Single operation with multiple aggregations
2. **Result Naming**: Preserve named aggregation results
3. **Binning Functions**: Represent time/numeric binning operations
4. **Parameter Support**: Handle summarize operator parameters

---

## Cross-Language Considerations

### Semantic Equivalence Mapping

| Operation | KQL Syntax | TraceQL Syntax | CommonAST Representation |
|-----------|------------|----------------|-------------------------|
| **Simple Count** | `\| summarize count()` | `{} \| count() > 0` | `AggregateOperation(count)` |
| **Grouped Count** | `\| summarize count() by field` | `{} \| by (field) \| count() > 0` | `GroupOperation + AggregateOperation` |
| **Time Series** | `\| summarize count() by bin(time, 1h)` | `{} \| count_over_time()` | `MetricsOperation` |
| **Multiple Aggregates** | `\| summarize count(), avg(x) by field` | `{} \| by (field) \| count() > 0`<br/>`{} \| by (field) \| avg(x) > 0` | Multiple operation nodes |

### Design Implications

1. **Normalization Strategy**: Convert all implicit grouping to explicit form
2. **Multi-Operation Support**: Handle KQL's multiple aggregations per operator
3. **Metadata Preservation**: Maintain result names and parameters
4. **Semantic Validation**: Ensure cross-language equivalence

### Translation Challenges

1. **KQL → TraceQL**: Split multi-aggregation summarize into multiple pipeline stages
2. **TraceQL → KQL**: Combine consecutive group + aggregate operations  
3. **Field Mapping**: Handle namespace differences (`span.field` vs `field`)
4. **Type Validation**: Ensure field types support requested aggregations

---

## Conclusion

This analysis reveals fundamental differences in aggregation semantics between TraceQL and KQL:

- **TraceQL**: Pipeline-based with explicit/implicit grouping duality
- **KQL**: Declarative with embedded grouping and rich function support

These differences drive the CommonAST design requirements:
1. **Flexible Grouping**: Support both explicit and embedded patterns
2. **Function Categories**: Distinguish between regular aggregates and metrics
3. **Cross-Language Normalization**: Unified representation for equivalent operations
4. **Extensibility**: Framework for future language additions

The next document will detail the specific CommonAST node designs to handle these requirements.
