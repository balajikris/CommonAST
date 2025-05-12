# CommonAst

## TODO
[x] what's kept and what's dropped
[x] e2e translation from kql query to ast.
[x] currently, the root node is a filter, extend the AST to support QueryNode, pipelineNodes and then filter as a child.
[] where - handling descendant and parent semantics in syntax. 
[x] How does kusto handle nested types, esp arrays?
[] project - implement.

## Design notes:
This implementation defines a class hierarchy that can represent filter expressions from both KQL and TraceQL.

The key aspects of this design include:

1. A common `INode` interface for all AST nodes with `NodeType` and `Location` properties
2. A `FilterNode` class that can represent both KQL's `whereOperator` and TraceQL's `SpansetFilter`
3. Various expression types like `BinaryExpression`, `Identifier`, and `Literal` to build complex filters
4. Special handling for namespace-qualified identifiers (e.g., `span.duration` in TraceQL)
5. A utility `AstBuilder` class with factory methods for conveniently creating AST nodes
6. Example methods showing how filters from both query languages would be represented

This design handles the differences between the languages by:

- Making the `Keyword` property in `FilterNode` optional (used for KQL but not for TraceQL)
- Supporting both plain identifiers and namespaced identifiers (for KQL and TraceQL respectively)
- Including support for special data types like `Duration` that are specific to TraceQL
- Being flexible enough to handle both languages' operator syntax
