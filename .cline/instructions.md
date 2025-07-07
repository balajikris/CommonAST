# CommonAST Project - Custom Instructions for Cline

## Project Overview

This is a C# .NET 8.0 console application that converts query languages (KQL and TraceQL) into a unified Common AST format. The project enables cross-language query analysis and transformation for distributed tracing and log analytics.

### Key Capabilities
- **KQL Parser**: Converts Kusto Query Language to Common AST using Microsoft.Azure.Kusto.Language
- **TraceQL Integration**: Planned support for TraceQL (distributed tracing queries)
- **Multi-Query Support**: Handles multiple queries separated by `$$` with span filters in `[]`
- **Graphviz Output**: Generates visual representations of both original syntax trees and Common AST
- **Dual Filtering**: Supports both trace-level and span-level filtering for distributed tracing scenarios

## Architecture Guidelines

### Common AST Design
The project uses a unified AST structure that can represent constructs from both KQL and TraceQL:

```csharp
// Core hierarchy
ASTNode (abstract base)
├── QueryNode (root of a complete query)
├── OperationNode (base for pipeline operations)
│   └── FilterNode (where/filter operations)
└── Expression (base for all expressions)
    ├── BinaryExpression, UnaryExpression, CallExpression
    ├── Literal, Identifier
    └── SpecialOperatorExpression, ParenthesizedExpression
```

### Query Pipeline Structure
- **QueryNode**: Root container with source and list of operations
- **Operations**: Pipeline of transformations (currently FilterNode, future: project, summarize, etc.)
- **FilterNode**: Handles both trace-level and span-level filtering

### Dual Filtering System
FilterNode supports two filtering levels:
- **TraceExpression**: Filters applied at trace level (backward compatible as `Expression` property)
- **SpanFilter**: Container for span-level expressions with combination logic (Any/All)

## Development Standards

### C# Conventions
- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled - use `?` for nullable types, `required` for mandatory properties
- **XML Documentation**: All public members must have `<summary>` documentation
- **Naming**: PascalCase for public members, camelCase for parameters and local variables

### Code Organization
```
src/
├── Program.cs                     # Entry point and CLI handling
├── AST/
│   ├── CommonAST.cs              # Core AST definitions and builders
│   ├── KqlToCommonAstVisitor.cs  # KQL conversion logic
│   ├── MultiQueryParser.cs       # Multi-query parsing
│   └── TraceQLBridge.cs          # TraceQL integration (planned)
├── Grammar/
│   ├── KQL.Grammar/              # ANTLR4 grammar files
│   └── TraceQL.Grammar/          # YACC grammar files
tests/
├── CommonAST.Tests/              # Main test suite
└── Testdata/                     # Test data files
```

### Testing Standards
- **Framework**: MSTest (`Microsoft.VisualStudio.TestTools.UnitTesting`)
- **Organization**: Group tests by functional area with `#region` blocks
- **Naming**: `MethodName_Condition_ExpectedResult()` pattern
- **Coverage**: Include edge cases, error conditions, and example validations
- **Assertions**: Use descriptive assertions with clear expected vs actual values

## Code Patterns & Best Practices

### AST Node Construction
Always use the `AstBuilder` static class for creating AST nodes:

```csharp
// Preferred
var query = AstBuilder.CreateQuery("MyTable");
var filter = AstBuilder.CreateFilter(expression, "where");

// For complex filtering
var combinedFilter = AstBuilder.CreateCombinedFilter(
    traceExpression, 
    spanExpressions, 
    SpanFilterCombination.Any
);
```

### Expression Building
Build expressions compositionally:

```csharp
var leftExpr = AstBuilder.CreateBinaryExpression(
    AstBuilder.CreateIdentifier("field"),
    BinaryOperatorKind.GreaterThan,
    AstBuilder.CreateLiteral(10, LiteralKind.Integer)
);

var rightExpr = AstBuilder.CreateBinaryExpression(
    AstBuilder.CreateIdentifier("status"),
    BinaryOperatorKind.Equal,
    AstBuilder.CreateLiteral("active", LiteralKind.String)
);

var combined = AstBuilder.CreateBinaryExpression(
    leftExpr, 
    BinaryOperatorKind.And, 
    rightExpr
);
```

### Visitor Pattern Implementation
When extending parsers or transformations, follow the visitor pattern:

```csharp
public class MyAstVisitor
{
    public void Visit(ASTNode node)
    {
        switch (node)
        {
            case QueryNode queryNode:
                VisitQuery(queryNode);
                break;
            case FilterNode filterNode:
                VisitFilter(filterNode);
                break;
            // ... handle other node types
        }
    }
}
```

### Error Handling
- **Parsing Errors**: Check diagnostics from KustoCode parsing
- **Validation**: Validate AST structure before processing
- **Graceful Degradation**: Handle missing or null expressions appropriately

## Domain Knowledge

### Query Language Concepts
- **KQL**: Kusto Query Language used in Azure Data Explorer, Application Insights
- **TraceQL**: Query language for distributed tracing (Jaeger, Tempo)
- **Pipeline Operations**: Queries are composed of operations connected by `|` (KQL) or combined filters (TraceQL)

### Distributed Tracing Terminology
- **Trace**: Complete request flow across multiple services
- **Span**: Individual operation within a trace
- **Span Filters**: Filters applied to individual spans within traces
- **Trace Filters**: Filters applied to entire traces

### Filter Semantics
- **Trace-level filtering**: Filter entire traces based on trace properties
- **Span-level filtering**: Filter traces based on span properties
- **Combination modes**: 
  - `Any`: Include trace if ANY span matches (OR semantics)
  - `All`: Include trace if ALL spans match (AND semantics)

## CLI Usage Patterns

The application supports these command patterns:
```bash
# Single query
CommonAST.exe "MyTable | where field > 10"

# Multi-query with span filters
CommonAST.exe "query1 $$ query2 [span.duration > 100ms]" --multi

# Custom output path
CommonAST.exe "query" --output "my_output"
```

## Testing Guidelines

### Test Organization
Group tests by functionality:
- **Basic Node Creation Tests**: Test individual AST node construction
- **Filter Node Tests**: Test FilterNode-specific functionality
- **Span Filter Tests**: Test dual filtering capabilities
- **Advanced Expression Types**: Test complex expression scenarios
- **Example Tests**: Validate examples from the Examples class

### Test Data Patterns
Use realistic query examples:
- KQL: `MyTable | where field > 10 and status == "active"`
- TraceQL: `{ span.duration > 100ms }`
- Combined: `{ trace.duration > 1s } && { span.name = "db" }`

### Edge Cases to Cover
- Null/empty expressions
- Mixed expression types in filters
- Complex nested binary expressions
- All literal types (String, Integer, Float, Boolean, Null, Duration, DateTime, Guid)
- Special operators (IN, BETWEEN, CONTAINS)

## Common Development Tasks

### Adding New AST Node Types
1. Add new `NodeKind` enum value
2. Create new class inheriting from appropriate base (ASTNode, Expression, OperationNode)
3. Add factory method to `AstBuilder`
4. Update visitor implementations
5. Add comprehensive tests
6. Update Graphviz generation if needed

### Implementing New Query Operations
1. Create new class inheriting from `OperationNode`
2. Add to `GenerateGraphvizForCommonAST` switch statement
3. Add factory method to `AstBuilder`
4. Add parser integration
5. Create comprehensive tests with examples

### Extending Parser Support
1. Update grammar files in `Grammar/` directory
2. Implement visitor methods for new syntax
3. Add conversion logic to appropriate visitor class
4. Add integration tests
5. Update CLI help text if needed

## Performance Considerations

- Use `List<T>` for collections that will be modified
- Avoid unnecessary object creation in tight loops
- Consider memory usage for large AST trees
- Use appropriate data structures for visitor pattern traversal

## Debugging Tips

- Use Graphviz output to visualize AST structure
- Check KustoCode diagnostics for parsing errors
- Use debugger to step through visitor pattern execution
- Validate AST structure before processing

## Future Roadmap Items

Based on TODO items in docs/MyNotes.md:
- **TraceQL Integration**: Complete TraceQL parsing and AST conversion
- **Project Operations**: Implement project/select operations
- **Nested Syntax**: Handle descendant/parent semantics
- **Performance**: Optimize for large query processing

## Common Pitfalls to Avoid

1. **Null Reference Exceptions**: Always check for null before accessing properties
2. **Expression Compatibility**: Ensure expressions are compatible with both KQL and TraceQL semantics
3. **Visitor Pattern**: Remember to handle all node types in visitor implementations
4. **Test Coverage**: Don't forget edge cases and error conditions
5. **Graphviz Generation**: Update visualization code when adding new node types

This project bridges two different query languages into a unified representation, so always consider the semantic differences and ensure the Common AST can represent constructs from both languages accurately.
