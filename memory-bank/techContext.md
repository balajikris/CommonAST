# Technical Context

## Technology Stack

### Core Framework
- **Primary**: .NET 8.0 (C#)
- **Project Type**: Console Application
- **Target Runtime**: Cross-platform (Windows, Linux, macOS)
- **Language Features**: C# 12 with nullable reference types enabled

### Dependencies & Libraries

#### Core Dependencies
- **Microsoft.Azure.Kusto.Language** (v11.7.6)
  - Official KQL parsing library from Microsoft
  - Provides KustoCode, SyntaxNode, and diagnostic capabilities
  - Handles complex KQL parsing and validation

#### Development Dependencies
- **Microsoft.VisualStudio.TestTools.UnitTesting** (MSTest)
  - Unit testing framework
  - Integrated with Visual Studio and .NET tooling
  - Supports test discovery and execution

### Grammar & Parsing Tools

#### KQL Grammar
- **ANTLR4**: Grammar definition tool
- **Files**: 
  - `src/Grammar/KQL.Grammar/Kql.g4` - Main grammar
  - `src/Grammar/KQL.Grammar/KqlTokens.g4` - Token definitions
- **Integration**: Used by Microsoft.Azure.Kusto.Language

#### TraceQL Grammar (Planned)
- **YACC**: Yet Another Compiler Compiler
- **Files**:
  - `src/Grammar/TraceQL.Grammar/traceql.yacc` - Grammar definition
  - `src/Grammar/TraceQL.Grammar/traceqltokens.js` - Token definitions
- **Status**: Planned implementation

### External Tools

#### Visualization
- **Graphviz**: Graph visualization software
- **Output Format**: DOT language files
- **Usage**: Generate visual representations of AST structures
- **Installation**: Required for viewing generated visualizations

#### Development Tools
- **Visual Studio/VS Code**: Primary development environment
- **Git**: Version control system
- **NuGet**: Package management

## Development Environment Setup

### Prerequisites
1. **.NET 8.0 SDK**
   - Required for building and running the application
   - Includes C# 12 compiler and runtime

2. **Visual Studio 2022** or **VS Code**
   - Recommended IDEs with C# support
   - IntelliSense, debugging, and testing integration

3. **Git**
   - Version control and collaboration
   - Repository management

4. **Graphviz** (Optional for visualization)
   - For rendering generated DOT files
   - Available via package managers or direct download

### Project Structure
```
CommonAST/
├── src/
│   ├── CommonAST.csproj          # Main project file
│   ├── Program.cs                # Entry point
│   ├── AST/                      # AST-related classes
│   │   ├── CommonAST.cs          # Core AST definitions
│   │   ├── KqlToCommonAstVisitor.cs
│   │   ├── MultiQueryParser.cs
│   │   └── TraceQLBridge.cs
│   ├── Grammar/                  # Grammar files
│   │   ├── KQL.Grammar/
│   │   └── TraceQL.Grammar/
│   └── bin/, obj/                # Build outputs
├── tests/
│   ├── CommonAST.Tests/          # Unit tests
│   │   ├── CommonAST.Tests.csproj
│   │   ├── CommonASTTests.cs
│   │   └── MultiQueryParserTests.cs
│   └── Testdata/                 # Test data files
├── docs/
│   └── MyNotes.md               # Development notes
├── tools/
│   └── purge-vscode-workspace.ps1
├── .gitignore
├── CommonAST.sln                # Solution file
└── README.md
```

## Build & Runtime Configuration

### Project Configuration
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### Key Settings
- **Nullable Reference Types**: Enabled for improved null safety
- **Implicit Usings**: Enabled for cleaner code
- **Output Type**: Executable console application
- **Target Framework**: .NET 8.0 for latest features and performance

## Technical Constraints

### Performance Constraints
- **Memory Usage**: Must handle reasonable query sizes without excessive memory consumption
- **Processing Time**: Should provide interactive response times for typical queries
- **Scalability**: Architecture should support future optimization for larger workloads

### Compatibility Constraints
- **Platform**: Must work on Windows, Linux, and macOS
- **Dependencies**: Minimize external dependencies for easier deployment
- **Versioning**: Compatible with .NET 8.0 and later versions

### Language Constraints
- **KQL Compatibility**: Must support full KQL syntax as parsed by Microsoft.Azure.Kusto.Language
- **TraceQL Compatibility**: Must support standard TraceQL syntax when implemented
- **Extension**: Architecture must allow for additional query languages

## Development Tools & Patterns

### Code Quality Tools
- **Built-in Analyzers**: C# compiler warnings and suggestions
- **XML Documentation**: Required for all public members
- **Nullable Reference Types**: Compile-time null safety
- **Code Reviews**: Manual review process for all changes

### Testing Strategy
- **Unit Tests**: MSTest framework with comprehensive coverage
- **Integration Tests**: End-to-end testing of parsing and conversion
- **Test Data**: Realistic query examples for validation
- **Continuous Testing**: Tests run on every build

### Documentation Tools
- **XML Comments**: Inline documentation for APIs
- **Markdown Files**: Project documentation and notes
- **Graphviz**: Visual documentation of AST structures
- **README**: User-facing documentation

## Integration Points

### Microsoft.Azure.Kusto.Language Integration
```csharp
// Parse KQL query
var code = KustoCode.Parse(query);

// Check for syntax errors
var diagnostics = code.GetSyntaxDiagnostics();
if (diagnostics.Count > 0)
{
    // Handle parsing errors
}

// Access syntax tree
var syntaxTree = code.Syntax;
```

### Graphviz Integration
```csharp
// Generate DOT file
using (var writer = new StreamWriter(outputPath))
{
    writer.WriteLine("digraph syntax_tree {");
    GenerateGraphvizForCommonAST(commonAst, writer);
    writer.WriteLine("}");
}
```

### File System Integration
- **Input**: Command-line arguments and file paths
- **Output**: DOT files for visualization
- **Configuration**: Command-line switches for behavior control

## Performance Considerations

### Memory Management
- **AST Construction**: Efficient node creation and linking
- **Visitor Pattern**: Minimize object allocation during traversal
- **String Handling**: Efficient string operations for large queries
- **Garbage Collection**: Minimize pressure on GC

### Processing Efficiency
- **Parser Reuse**: Leverage efficient Microsoft parser for KQL
- **Caching**: Minimize redundant parsing and processing
- **Streaming**: Process large inputs without loading everything in memory
- **Parallelization**: Potential for parallel processing of multi-queries

## Security Considerations

### Input Validation
- **Query Sanitization**: Validate input queries before processing
- **Path Validation**: Secure file path handling for output
- **Resource Limits**: Prevent excessive resource consumption

### Dependencies
- **Microsoft Libraries**: Trusted official libraries from Microsoft
- **NuGet Packages**: Use only well-maintained, trusted packages
- **Updates**: Keep dependencies updated for security patches

## Deployment Considerations

### Build Outputs
- **Executable**: Self-contained console application
- **Dependencies**: NuGet packages included in output
- **Configuration**: Minimal external configuration required

### Distribution
- **Package Format**: NuGet package or standalone executable
- **Installation**: Simple copy-and-run or package manager installation
- **Documentation**: Clear installation and usage instructions

### Runtime Requirements
- **.NET Runtime**: .NET 8.0 runtime required on target machine
- **Operating System**: Cross-platform compatibility
- **Memory**: Reasonable memory requirements for typical usage

This technical foundation provides a solid base for building a robust, maintainable, and extensible query parsing and AST transformation system.
