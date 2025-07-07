# CommonAST Project Brief

## Project Identity
- **Name**: CommonAST
- **Version**: 1.0.0 (Development)
- **Type**: C# .NET 8.0 Console Application
- **Domain**: Query Language Processing & AST Transformation

## Core Purpose
Convert query languages (KQL and TraceQL) into a unified Common AST format to enable cross-language query analysis, transformation, and visualization for distributed tracing and log analytics.

## Primary Goals
1. **Unified Representation**: Create a single AST structure that can represent constructs from both KQL and TraceQL
2. **Cross-Language Analysis**: Enable analysis and transformation of queries across different query languages
3. **Visualization**: Generate visual representations of both original syntax trees and Common AST
4. **Extensibility**: Design architecture that can accommodate additional query languages in the future

## Target Users
- **Primary**: Developers working with distributed tracing systems
- **Secondary**: Data engineers analyzing log queries
- **Tertiary**: Query language researchers and tool builders

## Key Requirements

### Functional Requirements
- Parse KQL queries using Microsoft.Azure.Kusto.Language
- Parse TraceQL queries (custom implementation)
- Convert both to unified Common AST format
- Support multi-query parsing with `$$` separators
- Handle span filters with `[]` syntax
- Generate Graphviz visualizations
- Provide CLI interface for processing queries

### Technical Requirements
- Target .NET 8.0 framework
- Use nullable reference types
- Comprehensive XML documentation
- Full test coverage with MSTest
- Support for both single and multi-query processing
- Error handling and validation

### Quality Requirements
- Maintainable and extensible architecture
- Clear separation of concerns
- Comprehensive test coverage
- Clear documentation and examples
- Performance suitable for interactive use

## Core Concepts

### Query Languages
- **KQL (Kusto Query Language)**: Used in Azure Data Explorer, Application Insights
- **TraceQL**: Query language for distributed tracing (Jaeger, Tempo)

### AST Architecture
- **QueryNode**: Root container for complete queries
- **OperationNode**: Base for pipeline operations (filter, project, etc.)
- **Expression**: Base for all expressions (binary, unary, literals, etc.)
- **Dual Filtering**: Support for both trace-level and span-level filtering

### Processing Pipeline
1. Parse source query using appropriate parser
2. Convert to Common AST using visitor pattern
3. Generate visualization outputs
4. Support multi-query scenarios with span filtering

## Success Criteria
- Successfully parse and convert KQL queries to Common AST
- Implement TraceQL parsing and conversion
- Generate accurate Graphviz visualizations
- Handle complex multi-query scenarios
- Maintain high code quality and test coverage
- Provide clear documentation and examples

## Project Scope

### In Scope
- KQL parsing and AST conversion
- TraceQL parsing and AST conversion
- Common AST structure design
- Graphviz visualization generation
- Multi-query processing
- Span filtering support
- CLI interface
- Comprehensive testing

### Out of Scope (Initially)
- Round-trip serialization (explicitly dropped)
- Complex query optimization
- Performance optimization for very large queries
- Web interface or GUI
- Query execution capabilities
- Integration with specific databases or systems

## Dependencies
- Microsoft.Azure.Kusto.Language (KQL parsing)
- .NET 8.0 framework
- Graphviz (for visualization)
- ANTLR4 (for grammar processing)
- MSTest (for testing)

This project serves as a foundation for cross-language query processing and analysis in the distributed tracing and log analytics domain.
