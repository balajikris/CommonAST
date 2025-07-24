# TraceQL Syntax Analysis & Findings

## Current Parser Status

### ✅ **What Works (Parser is COMPLETE for these features)**
- **Basic spanset filtering**: `{ span.name = "http.request" }`
- **Pipeline operations**: `|` operator for chaining
- **Aggregate functions**: `sum()`, `avg()`, `min()`, `max()`, `count()`
- **Metrics over time**: `sum_over_time()`, `avg_over_time()`, etc.
- **Group operations**: `by (field)` clauses
- **Complex expressions**: Comparisons, logical operators, etc.

### ❌ **What Doesn't Work (Not in TraceQL spec)**
- **Standalone aggregates**: `sum by (service)` without function parameters

## Correct TraceQL Syntax Examples

### Working Queries (all parse successfully):

```traceql
# Basic filtering
{ span.name = "http.request" }
{ span.duration > 100ms }

# Aggregation with comparison
{ true } | sum(duration) = 1h
{ span.service = "web" } | avg(duration) > 500ms

# Metrics over time with grouping
{} | sum_over_time(resource.service) by (span.name)
{} | avg_over_time(span.duration) by (resource.cluster)

# Complex filtering with aggregation
{ span.name = "api-call" && span.status = "ok" } | count() > 100

# other examples
# Sum over time with grouping
{} | sum_over_time(span.latency) by (resource.service)

# Aggregate with comparison
{} | sum(span.latency) > 1s by (resource.service)

# If latency is a custom attribute
{} | sum_over_time(.latency) by (resource.service)

# Filters then aggregates with grouping
{ span.service = "api" } | avg_over_time(span.duration) by (resource.cluster)

```

## Parser Capabilities Assessment

### ✅ **Fully Supported TraceQL Features**
1. **Spanset Filtering**: `{}`, `{ field = value }`
2. **Pipeline Operations**: `|` chaining
3. **Aggregate Functions**:
   - `sum(field)`
   - `avg(field)`
   - `min(field)`
   - `max(field)`
   - `count()`
4. **Metrics Operations**:
   - `sum_over_time(field)`
   - `avg_over_time(field)`
   - `min_over_time(field)`
   - `max_over_time(field)`
   - `count_over_time()`
   - `rate()`
   - `histogram_over_time(field)`
   - `quantile_over_time(q, field)`
5. **Grouping**: `by (field1, field2, ...)`
6. **Comparison Operators**: `=`, `!=`, `>`, `<`, `>=`, `<=`, `=~`, `!~`
7. **Logical Operators**: `&&`, `||`, `!`
8. **Field Types**:
   - **Intrinsic**: `duration`, `name`, `span:duration`, etc.
   - **Attribute**: `span.field`, `resource.field`, `.field`
9. **Data Types**: String, Integer, Float, Duration, Boolean
10. **Advanced Operations**:
    - **Structural**: `>`, `>>`, `<`, `<<`, `~`
    - **Select**: `select(field1, field2)`
    - **Coalesce**: `coalesce()`

### ❌ **Not Supported (Not in TraceQL)**
- **Standalone `sum by`**: Must be `sum(field) by` or `sum_over_time(field) by`

## 📚 **TraceQL Syntax Reference**

### **Core Operations**:
```traceql
# Spanset filtering
{ field = "value" }
{ span.name = "api" && span.duration > 100ms }

# Pipeline chaining
{} | operation1 | operation2

# Aggregation functions
sum(field)    avg(field)    min(field)    max(field)    count()

# Metrics over time  
sum_over_time(field)    avg_over_time(field)    rate()

# Grouping
by (field1, field2, ...)

# Complete example
{ span.service = "web" } | sum_over_time(span.duration) by (resource.cluster)
```

### **Field Types**:
```traceql
# Intrinsic fields
duration, name, span:duration, trace:id, etc.

# Attribute fields
span.field          # Span attributes
resource.field      # Resource attributes  
.field             # Any attribute
```