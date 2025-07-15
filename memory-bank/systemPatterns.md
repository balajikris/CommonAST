# System Patterns

## Overall Architecture

### Layered Architecture
The system follows a clean layered architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                    CLI Interface Layer                       │
│                      (Program.cs)                           │
├─────────────────────────────────────────────────────────────┤
│                    Processing Layer                          │
│           (KqlToCommonAstVisitor, MultiQueryParser)         │
├─────────────────────────────────────────────────────────────┤
│                    AST Model Layer                           │
│         (CommonAST.cs - Nodes, Builders, Examples)          │
├─────────────────────────────────────────────────────────────┤
│                    Parser Integration Layer                   │
│    (Microsoft.Azure.Kusto.Language, TraceQL Grammar)        │
└─────────────────────────────────────────────────────────────┘
```

### Component Relationships
- **CLI → Processing**: Program.cs orchestrates parsing and conversion
- **Processing → AST Model**: Visitors create AST nodes using builders
- **Processing → Parser Integration**: Leverages external parsers for language-specific parsing
- **AST Model → Visualization**: Generates Graphviz DOT files for developer diagnosis
- **AST Model → Expression Evaluation Engine**: Provides engine-agnostic AST for Arrow data operations

## Key Design Patterns

### 1. Abstract Syntax Tree (AST) Pattern
**Purpose**: Represent parsed queries in a language-agnostic tree structure

**Implementation**:
```csharp
// Base abstraction
public abstract class ASTNode
{
    public abstract NodeKind NodeKind { get; }
}

// Specialized nodes
public class QueryNode : ASTNode
public class FilterNode : OperationNode
public class BinaryExpression : Expression
```

**Benefits**:
- Unified representation across languages
- Easy traversal and transformation
- Extensible for new node types

### 2. Visitor Pattern
**Purpose**: Traverse and transform AST structures without modifying node classes

**Implementation**:
```csharp
public class KqlToCommonAstVisitor
{
    public void Visit(SyntaxNode node)
    {
        // Language-specific traversal logic
        switch (node.Kind)
        {
            case SyntaxKind.WhereOperator:
                VisitWhereOperator(node);
                break;
            // ... other cases
        }
    }
}
```

**Benefits**:
- Separation of concerns
- Easy to add new transformations
- Doesn't pollute AST node classes

### 3. Builder Pattern
**Purpose**: Provide consistent, fluent interface for AST construction

**Implementation**:
```csharp
public static class AstBuilder
{
    public static QueryNode CreateQuery(string? source = null)
    public static FilterNode CreateFilter(Expression expression, string? keyword = null)
    public static BinaryExpression CreateBinaryExpression(Expression left, BinaryOperatorKind op, Expression right)
}
```

**Benefits**:
- Consistent object creation
- Reduces complexity
- Centralizes validation logic

### 4. Factory Pattern
**Purpose**: Create appropriate parsers and converters based on input type

**Implementation**:
```csharp
// Multi-query parsing factory logic
if (useMultiQueryParser)
{
    commonAst = MultiQueryParser.Parse(query);
}
else
{
    var code = KustoCode.Parse(query);
    var visitor = new KqlToCommonAstVisitor();
    // ...
}
```

**Benefits**:
- Encapsulates creation logic
- Easy to add new language support
- Centralized parser selection

### 5. Strategy Pattern
**Purpose**: Support different parsing strategies for different query languages

**Implementation**:
- KQL Strategy: Use Microsoft.Azure.Kusto.Language
- TraceQL Strategy: Custom YACC-based parser
- Multi-Query Strategy: Combined parsing with span filters

**Benefits**:
- Pluggable parsing approaches
- Easy to add new languages
- Isolated parsing logic

## Critical Implementation Paths

### 1. Single Query Processing Path
```
Input Query → KustoCode.Parse() → SyntaxNode Tree → 
KqlToCommonAstVisitor → Common AST → Graphviz Generation
```

**Key Components**:
- `KustoCode.Parse()`: Microsoft's KQL parser
- `KqlToCommonAstVisitor`: Custom visitor for AST conversion
- `GenerateGraphvizForCommonAST()`: Visualization generation

### 2. Multi-Query Processing Path
```
Multi-Query Input → MultiQueryParser.Parse() → 
Combined AST with Span Filters → Graphviz Generation
```

**Key Components**:
- `MultiQueryParser`: Custom parser for $$ separator and [] span filters
- Span filter parsing and AST integration
- Combined AST generation

### 3. AST Construction Path
```
Raw Syntax Elements → AstBuilder Factory Methods → 
Typed AST Nodes → Validation → Final AST Structure
```

**Key Components**:
- `AstBuilder` factory methods
- Node validation logic
- Type-safe AST construction

## Component Interaction Patterns

### 1. CLI Orchestration Pattern
The CLI acts as the main orchestrator:

```csharp
public static void Main(string[] args)
{
    // 1. Parse command line arguments
    // 2. Select appropriate parsing strategy
    // 3. Execute parsing and conversion
    // 4. Generate outputs
    // 5. Handle errors and provide feedback
}
```

### 2. Visitor Traversal Pattern
Systematic traversal of syntax trees:

```csharp
// Top-down traversal
public void Visit(SyntaxNode node)
{
    // 1. Handle current node
    // 2. Process children recursively
    // 3. Build AST bottom-up
}
```

### 3. Builder Composition Pattern
Compositional AST construction:

```csharp
// Build complex expressions from simple parts
var leftExpr = AstBuilder.CreateBinaryExpression(/*...*/);
var rightExpr = AstBuilder.CreateBinaryExpression(/*...*/);
var combined = AstBuilder.CreateBinaryExpression(leftExpr, BinaryOperatorKind.And, rightExpr);
```

## Architectural Decisions

### 1. Unified AST Over Multiple ASTs
**Decision**: Use single AST structure for all query languages
**Rationale**: Enables cross-language analysis and tool building
**Trade-off**: Some language-specific nuances may be lost

### 2. Dual Filtering Architecture
**Decision**: Support both trace-level and span-level filtering
**Rationale**: Matches TraceQL semantics while maintaining KQL compatibility
**Implementation**: FilterNode with TraceExpression and SpanFilter properties

### 3. Builder Pattern for Construction
**Decision**: Use static factory methods rather than constructors
**Rationale**: Provides consistent interface and validation
**Benefit**: Reduces complexity and improves maintainability

### 4. Visitor Pattern for Transformation
**Decision**: Use visitor pattern for syntax tree transformation
**Rationale**: Keeps transformation logic separate from AST structure
**Benefit**: Easy to add new transformations without modifying nodes

### 5. External Parser Integration
**Decision**: Use Microsoft.Azure.Kusto.Language for KQL parsing
**Rationale**: Leverages official, well-tested parser
**Benefit**: Reduces implementation complexity and improves reliability

## Critical Design Considerations

### Grammar-Driven AST Design
**CRITICAL**: When adding support for new language constructs (e.g., project operations), the process must be:

1. **Grammar Analysis**: Examine both KQL and TraceQL grammar files
   - `src/Grammar/KQL.Grammar/Kql.g4` and `KqlTokens.g4`
   - `src/Grammar/TraceQL.Grammar/traceql.yacc` and `traceqltokens.js`
   
2. **Cross-Language Mapping**: Identify equivalent constructs between languages
   - Find common data processing operations
   - Identify language-specific variations
   - Determine what can be unified in AST

3. **Engine-Agnostic Filter**: Exclude engine-specific constructs
   - **Exclude**: Kusto engine-specific operations that don't apply to data processing
   - **Include**: Data processing operations (filter, project, aggregate, etc.)
   - **Focus**: Operations that can execute on Arrow data via Expression Evaluation engine

4. **AST Structure Design**: Create unified representation
   - Design AST nodes that accommodate both languages
   - Ensure compatibility with Arrow data operations
   - Plan for Expression Evaluation engine execution

### Engine-Specific Exclusion Pattern
**Rule**: Any language construct that applies only to a specific query engine (e.g., Kusto engine) must be excluded from the Common AST.

**Examples of Exclusions**:
- Kusto-specific administrative commands
- Engine-specific optimization hints
- Platform-specific data source references
- Engine-specific functions not applicable to Arrow data

## Extension Points

### 1. New Query Language Support
- Add new visitor class (e.g., `SparkSqlToCommonAstVisitor`)
- Implement parser integration
- Add factory logic for language detection
- Update CLI to support new format

### 2. New AST Node Types (Grammar-Driven Process)
- **Step 1**: Analyze grammar files for both KQL and TraceQL
- **Step 2**: Identify common data processing patterns
- **Step 3**: Design engine-agnostic AST representation
- **Step 4**: Add NodeKind enum value
- **Step 5**: Create new class inheriting from appropriate base
- **Step 6**: Add factory methods to AstBuilder
- **Step 7**: Update visitor implementations
- **Step 8**: Add Graphviz generation support

### 3. New Operation Types
- Create new class inheriting from OperationNode
- Add to query pipeline processing
- Update visualization generation
- Add comprehensive tests

### 4. New Expression Types
- Create new class inheriting from Expression
- Add to expression processing logic
- Update all visitor implementations
- Add builder factory methods

## Quality Patterns

### 1. Test Organization Pattern
Tests are organized by functional area:
- Basic Node Creation Tests
- Filter Node Tests
- Span Filter Tests
- Advanced Expression Types
- Example Tests

### 2. Documentation Pattern
All public members have XML documentation:
```csharp
/// <summary>
/// Creates a binary expression with left operand, operator, and right operand
/// </summary>
/// <param name="left">Left operand expression</param>
/// <param name="op">Binary operator</param>
/// <param name="right">Right operand expression</param>
/// <returns>Binary expression AST node</returns>
public static BinaryExpression CreateBinaryExpression(Expression left, BinaryOperatorKind op, Expression right)
```

### 3. Error Handling Pattern
Consistent error handling across components:
- Parse errors from KustoCode diagnostics
- Validation errors from AST construction
- Clear error messages with actionable guidance

This architecture provides a solid foundation for cross-language query processing while maintaining extensibility for future enhancements.

## Visitor Pattern Requirements for New AST Constructs

**CRITICAL**: When implementing new AST node types, the visitor pattern must be updated to handle the new constructs. This is essential for complete implementation.

### Required Visitor Updates

When adding a new AST construct (like ProjectNode), you must update **ALL** visitor implementations:

1. **KqlToCommonAstVisitor** (most critical)
   - Add new case to the `Visit(SyntaxNode node)` switch statement
   - Implement the corresponding `VisitXxxOperator()` method
   - Handle language-specific syntax parsing
   - Convert to Common AST representation

2. **Future Visitors** (TraceQL, others)
   - Any future visitor implementations must also support the new constructs
   - Follow the same pattern for consistency

### Example: Adding ProjectNode Support

**Step 1**: Add case to switch statement
```csharp
case SyntaxKind.ProjectOperator:
    VisitProjectOperator(node as ProjectOperator);
    break;
```

**Step 2**: Implement visitor method
```csharp
private void VisitProjectOperator(ProjectOperator node)
{
    // Parse KQL project syntax
    // Extract expressions and aliases
    // Convert to Common AST ProjectNode
    // Add to query operations
}
```

**Step 3**: Handle language-specific nuances
- KQL: `| project field1, alias = field2, calculation = field3 / 1000`
- TraceQL: `select(span.field1, span.field2)` (different syntax, same concept)

### Common Visitor Patterns

**Expression Stack Pattern**: Use stack to handle nested expressions
```csharp
private Stack<Expression> _expressionStack = new Stack<Expression>();

// In visitor methods:
Visit(childExpression);
if (_expressionStack.Count > 0)
{
    var expr = _expressionStack.Pop();
    // Use expression
}
```

**Separated Elements Pattern**: Handle comma-separated lists
```csharp
foreach (var separatedElement in node.Expressions)
{
    var actualExpression = separatedElement.Element;
    Visit(actualExpression);
}
```

**Alias Handling Pattern**: Extract optional aliases from syntax
```csharp
if (column is SimpleNamedExpression namedExpr)
{
    string? alias = namedExpr.Name?.SimpleName;
    // Process with alias
}
else
{
    // Process without alias
}
```

### Testing Visitor Implementation

Always test visitor updates with:
1. **Simple queries**: Basic syntax verification
2. **Complex queries**: Nested expressions, multiple operations
3. **Edge cases**: Empty lists, null expressions
4. **Integration tests**: End-to-end query processing

### Memory Bank Update Requirement

When adding visitor support for new constructs, **MUST** update:
- `memory-bank/progress.md`: Mark visitor support as complete
- `memory-bank/systemPatterns.md`: Document any new patterns
- `memory-bank/designProcess.md`: Capture design decisions

This ensures future developers understand the complete implementation requirements for new AST constructs.
