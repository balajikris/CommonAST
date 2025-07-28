# KQL Tester Tool

The KQL Tester is an interactive command-line tool for testing, debugging, and understanding KQL (Kusto Query Language) queries. It provides visual parse tree representations, syntax validation, and CommonAST conversion capabilities.

## Features

- 🔍 **Parse Tree Visualization** - Visual representation of KQL syntax trees
- ✅ **Syntax Validation** - Real-time error detection and reporting
- 🌳 **CommonAST Conversion** - Convert KQL to unified AST format
- 📁 **File Input Support** - Test queries from external files
- 🎮 **Interactive Mode** - Real-time query testing and feedback
- 📊 **Multiple Output Formats** - Text, JSON, SVG visualization support

## Installation & Setup

The KQL Tester is integrated into the CommonAST project. Build the project to use the tool:

```bash
cd src
dotnet build
```

## Basic Usage

### Command Syntax

```bash
CommonAST.exe kql-test [options] <query|--file|--interactive>
```

### Quick Start Examples

```bash
# Test a simple KQL query
CommonAST.exe kql-test "MyTable | where timestamp > ago(1h)"

# Show CommonAST output
CommonAST.exe kql-test "Events | where Level == 'Error'" --ast

# Test from file
CommonAST.exe kql-test --file my-query.kql

# Interactive mode
CommonAST.exe kql-test --interactive
```

## Command-Line Options

### Input Options
- `"query"` - KQL query string to test
- `--file <filename>` - Read query from file
- `--interactive` - Start interactive mode

### Output Options
- `--ast` - Show CommonAST structure
- `--json` - Format AST output as JSON
- `--svg` - Generate SVG visualization (integrates with existing Graphviz)
- `--quiet` - Suppress informational output
- `--output <filename>` - Specify output filename for SVG

## Example Queries

### Basic Filtering
```kql
# Simple table filtering
MyTable | where timestamp > ago(1h)

# Multiple conditions
Events | where Level == "Error" and EventId > 1000

# String operations
Messages | where Text contains "error" or Text startswith "warn"
```

### Aggregation
```kql
# Count by category
Logs | summarize count() by Level

# Time-based aggregation
Metrics | summarize avg(Value) by bin(timestamp, 1h)

# Multiple aggregations
Data | summarize count(), avg(Duration), max(Size) by Category
```

### Advanced Queries
```kql
# Time range filtering
Traces | where timestamp between(ago(2h) .. ago(1h))

# Numeric comparisons
Metrics | where Value > 100 and Value < 1000

# Function calls with validation
Data | where isnotnull(UserId) and strlen(Name) > 5
```

## Interactive Mode

Start interactive mode to test queries in real-time:

```bash
CommonAST.exe kql-test --interactive
```

### Interactive Commands

| Command | Description |
|---------|-------------|
| `help` | Show available commands |
| `examples` | Display example queries |
| `svg on/off` | Toggle SVG generation |
| `ast on/off` | Toggle CommonAST output |
| `json on/off` | Toggle JSON AST format |
| `exit` or `quit` | Exit interactive mode |

### Interactive Example Session

```
KQL> help

Available commands:
  help          Show this help
  svg on/off    Toggle SVG generation (currently OFF)
  ast on/off    Toggle CommonAST output (currently OFF)
  json on/off   Toggle JSON AST format (currently OFF)
  examples      Show example queries
  exit/quit     Exit the tester

KQL> ast on
✅ CommonAST output enabled

KQL> Events | where Level == "Error"
============================================================
Testing KQL Query: Events | where Level == "Error"
============================================================

📋 KQL Parse Tree:
└─ QueryBlock
   └─ List
      └─ SeparatedElement
         └─ ExpressionStatement
            └─ PipeExpression
               ├─ NameReference (Events)
               └─ FilterOperator
                  └─ EqualExpression (Level == "Error")

✅ Query compiled successfully!

🌳 CommonAST Structure:
└─ Query (Source: Events)
   └─ Filter (Keyword: filter)
      └─ BinaryExpression (Equal)
         ├─ Identifier (Level)
         └─ Literal (String: "Error")
```

## Output Formats

### Text Tree Format (Default)
- Clean visual representation using box-drawing characters
- Shows node types and relevant information
- Error markers (⚠) highlight syntax issues

### JSON Format
- Machine-readable AST representation
- Useful for programmatic processing
- Includes all node properties and relationships

### CommonAST Format
- Unified AST structure compatible with TraceQL
- Engine-agnostic representation
- Suitable for cross-language analysis

## Error Handling

The KQL Tester provides comprehensive error reporting:

### Syntax Error Example
```bash
$ CommonAST.exe kql-test "invalid query syntax here"

KQL Parse Tree (with errors):
└─ QueryBlock
   ├─ NameReference (invalid) ⚠
   ├─ NameReference (query) ⚠
   └─ NameReference (syntax) ⚠

❌ Syntax errors found:
   ⚠ Expected: ; (at position 8)
   ⚠ Expected: ; (at position 14)

❌ Query has syntax errors
   Please check the query syntax and try again.
```

## File Input

Create KQL files for batch testing:

**example.kql**
```kql
MyTable 
| where timestamp > ago(2h)
| where Level == "Error"
| summarize count() by bin(timestamp, 1h)
```

**Test the file:**
```bash
CommonAST.exe kql-test --file example.kql --ast --json
```

## Integration with CommonAST Project

The KQL Tester integrates seamlessly with the existing CommonAST functionality:

### Standard Mode (Existing)
```bash
# Generate Graphviz files
CommonAST.exe "MyTable | where x > 10" --output my-query

# Multi-query parsing
CommonAST.exe "query1 $$ query2" --multi
```

### KQL Tester Mode (New)
```bash
# Interactive testing and debugging
CommonAST.exe kql-test --interactive

# Quick validation
CommonAST.exe kql-test "query" --quiet
```

## Common Use Cases

### 1. Query Development
- Test queries during development
- Validate syntax before deployment
- Understand query structure and execution

### 2. Learning KQL
- Explore example queries
- See how different syntax elements are parsed
- Understand error messages and corrections

### 3. Debugging
- Identify syntax errors in complex queries
- Visualize query structure for optimization
- Validate query logic before execution

### 4. Cross-Language Analysis
- Convert KQL to CommonAST for comparison with TraceQL
- Analyze query patterns across different languages
- Build tools that work with multiple query languages

## Troubleshooting

### Common Issues

**Issue: "File not found"**
```bash
# Ensure correct path
CommonAST.exe kql-test --file ./queries/my-query.kql
```

**Issue: Interactive mode not responding**
```bash
# Use Ctrl+C to exit if needed
# Restart with clean terminal session
```

**Issue: Unicode characters not displaying**
```bash
# Ensure terminal supports UTF-8 encoding
# Use --quiet flag to suppress visual elements if needed
```

### Getting Help

1. **Built-in Help**: `CommonAST.exe` (no arguments)
2. **Interactive Help**: Type `help` in interactive mode  
3. **Examples**: Type `examples` in interactive mode
4. **Project Documentation**: See main project README

## Performance Notes

- **Large Queries**: The tool handles complex queries efficiently
- **File Processing**: Supports multi-line queries from files
- **Memory Usage**: Optimized for interactive use
- **Response Time**: Fast parsing and validation for development workflows

## Future Enhancements

Planned features for future releases:
- Query execution simulation
- Performance analysis metrics
- Enhanced SVG visualization options
- Integration with external KQL engines
- Batch file processing capabilities

---

For more information about the CommonAST project and its capabilities, see the main project documentation.
