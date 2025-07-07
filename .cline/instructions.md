# CommonAST Project - Custom Instructions for Cline

## Project Overview

This is a C# .NET 8.0 console application that converts query languages (KQL and TraceQL) into a unified Common AST format that serves as the frontend for an Expression Evaluation (EE) engine. The EE engine executes AST operations on Arrow data in-memory, fetching parquet files from ADLS (Azure Data Lake Storage) as needed.

### Key Capabilities
- **KQL Parser**: Converts Kusto Query Language to Common AST using Microsoft.Azure.Kusto.Language
- **TraceQL Integration**: Planned support for TraceQL (distributed tracing queries)
- **Multi-Query Support**: Handles multiple queries separated by `$$` with span filters in `[]`
- **Developer Diagnostics**: Generates Graphviz DOT files for developer understanding of AST structure
- **Dual Filtering**: Supports both trace-level and span-level filtering for distributed tracing scenarios
- **Expression Evaluation Backend**: AST serves as frontend for EE engine that executes on Arrow data from ADLS

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

## Critical Design Requirements

### Grammar-Driven Development
**CRITICAL**: When adding support for new language constructs (e.g., project operations), you MUST follow this process:

1. **Grammar Analysis**: Examine both KQL and TraceQL grammar files
   - `src/Grammar/KQL.Grammar/Kql.g4` and `KqlTokens.g4`
   - `src/Grammar/TraceQL.Grammar/traceql.yacc` and `traceqltokens.js`

2. **Cross-Language Mapping**: Identify equivalent constructs between languages
   - Find common data processing operations
   - Identify language-specific variations
   - Determine what can be unified in AST

3. **Engine-Agnostic Filter**: Exclude engine-specific constructs
   - **EXCLUDE**: Kusto engine-specific operations (administrative commands, optimization hints)
   - **INCLUDE**: Data processing operations (filter, project, aggregate, etc.)
   - **FOCUS**: Operations that can execute on Arrow data via Expression Evaluation engine

4. **AST Structure Design**: Create unified representation
   - Design AST nodes that accommodate both languages
   - Ensure compatibility with Arrow data operations
   - Plan for Expression Evaluation engine execution

### Expression Evaluation Engine Compatibility
The Common AST must be designed for execution by the Expression Evaluation (EE) engine:
- **Arrow Data**: Operations must work with Arrow data structures
- **ADLS Integration**: Support parquet file access from Azure Data Lake Storage
- **In-Memory Processing**: Optimize for in-memory record batch operations
- **Engine Independence**: No dependency on specific query engines (e.g., Kusto)

### Graphviz Purpose
**Important**: Graphviz DOT file generation is solely for developer diagnosis and understanding of AST structure during development. It is NOT for end-user visualization or production use.

## Common Development Tasks

### Adding New AST Node Types (Grammar-Driven Process)
**CRITICAL**: Must follow grammar analysis process above before implementing:

1. **Grammar Analysis**: Examine both KQL and TraceQL grammar files first
2. **Engine-Agnostic Design**: Ensure compatibility with Expression Evaluation engine
3. Add new `NodeKind` enum value
4. Create new class inheriting from appropriate base (ASTNode, Expression, OperationNode)
5. Add factory method to `AstBuilder`
6. Update visitor implementations
7. Add comprehensive tests
8. Update Graphviz generation for developer diagnosis

### Implementing New Query Operations (Grammar-Driven Process)
**CRITICAL**: Must follow grammar analysis process above before implementing:

1. **Grammar Analysis**: Examine both language grammars for the operation
2. **Cross-Language Mapping**: Identify how operation works in both languages
3. **Engine-Agnostic Design**: Ensure operation can execute on Arrow data
4. Create new class inheriting from `OperationNode`
5. Add to `GenerateGraphvizForCommonAST` switch statement
6. Add factory method to `AstBuilder`
7. Add parser integration
8. Create comprehensive tests with examples

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
