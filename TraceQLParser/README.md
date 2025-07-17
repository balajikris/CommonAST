# TraceQL Parser

A TypeScript library for parsing TraceQL queries and generating parse tree visualizations.

## Features

### Phase 1: Text-Based Parse Tree Visualization ✅
- **`printParseTree(query: string): string`** - Returns formatted text representation of the parse tree
- **`logParseTree(query: string): void`** - Logs the parse tree to console for debugging
- Enhanced formatting with box-drawing characters and position information
- Graceful handling of invalid queries with error markers

### Phase 2: SVG/Graphviz Visualization ✅
- **`generateParseTreeDot(query: string): string`** - Generates Graphviz DOT representation
- **`generateParseTreeSvg(query: string): Promise<string>`** - Generates SVG representation
- **`saveParseTreeSvg(query: string, filename?: string): Promise<void>`** - Saves SVG to file
- Professional graph visualization with proper escaping and formatting
- Error nodes highlighted in different colors

### CommonAST Integration ✅
- **`parseTraceQL(query: string): FilterNode`** - Parses TraceQL to CommonAST structure
- **`parseTraceQLToCommonAST(query: string): string`** - JSON representation of CommonAST
- Full support for span filters, binary expressions, identifiers, and literals
- Proper handling of namespace (e.g., `span.name`, `span.duration`)

## Usage Examples

```typescript
import { printParseTree, logParseTree, saveParseTreeSvg, parseTraceQL } from './index';

// Text-based visualization
const query = '{ span.name = "http.request" }';
console.log(printParseTree(query));

// Console logging
logParseTree(query);

// SVG generation
await saveParseTreeSvg(query, 'my-query.svg');

// CommonAST parsing
const ast = parseTraceQL(query);
console.log(ast);
```

## Sample Output

### Text Visualization
```
TraceQL [0-30]: "{ span.name = "http.request" }"
├─ SpansetPipelineExpression [0-30]: "{ span.name = "http.request" }"
│  ├─ SpansetPipeline [0-30]: "{ span.name = "http.request" }"
│  │  ├─ SpansetFilter [0-30]: "{ span.name = "http.request" }"
│  │  │  ├─ FieldExpression [2-28]: "span.name = "http.request""
│  │  │  │  ├─ FieldExpression [2-11]: "span.name"
│  │  │  │  │  ├─ AttributeField [2-11]: "span.name"
│  │  │  │  │  │  ├─ Span [2-6]: "span"
│  │  │  │  │  │  ├─ Identifier [7-11]: "name"
│  │  │  │  ├─ FieldOp [12-13]: "="
│  │  │  │  ├─ FieldExpression [14-28]: ""http.request""
│  │  │  │  │  ├─ Static [14-28]: ""http.request""
│  │  │  │  │  │  ├─ String [14-28]: ""http.request""
```

### CommonAST Output
```json
{
  "nodeType": "Filter",
  "expression": {
    "nodeType": "BinaryExpression",
    "operator": "=",
    "left": {
      "nodeType": "Identifier",
      "name": "name",
      "namespace": "span"
    },
    "right": {
      "nodeType": "Literal",
      "valueType": "String",
      "value": "http.request"
    }
  }
}
```

## Dependencies

- `@grafana/lezer-traceql` - TraceQL parser
- `@hpcc-js/wasm` - Graphviz rendering
- `@lezer/common` - Parse tree utilities

## File Structure

```
TraceQLParser/
├── src/
│   ├── index.ts          # Main implementation
│   ├── index.test.ts     # Test suite
├── dist/                 # Compiled JavaScript
├── package.json
├── tsconfig.json
├── jest.config.js
└── .gitignore
```

## Test Coverage

All major functionality is covered by tests:
- ✅ Parse empty filters
- ✅ Parse attribute comparisons
- ✅ Parse duration comparisons
- ✅ Text tree generation
- ✅ Error handling
- ✅ Console logging

## Build & Test

```bash
npm install
npm run build
npm test
```

## TraceQL Tester

A comprehensive command-line tool for testing and understanding TraceQL queries.

### Basic Usage

```bash
# Test a query from file (recommended for complex queries)
node traceql-tester.js --file examples/basic.traceql

# Test a query string (shell-specific escaping)
# PowerShell/Windows:
node traceql-tester.js '{ span.name = "http.request" }'

# Bash/Linux/macOS:
node traceql-tester.js "{ span.name = \"http.request\" }"

# Interactive mode (easiest for testing)
node traceql-tester.js --interactive
```

### Shell Escaping Guide

Different shells handle quote escaping differently:

#### PowerShell (Windows)
```powershell
# Use single quotes to avoid escaping issues
node traceql-tester.js '{ span.name = "http.request" }'
node traceql-tester.js '{ span.duration > 100ms }'
```

#### Command Prompt (Windows)
```cmd
# Use double quotes and escape inner quotes
node traceql-tester.js "{ span.name = \"http.request\" }"
```

#### Bash/Zsh (Linux/macOS)
```bash
# Use double quotes and escape inner quotes
node traceql-tester.js "{ span.name = \"http.request\" }"
# Or use single quotes
node traceql-tester.js '{ span.name = "http.request" }'
```

**💡 Tip**: For complex queries with quotes, use `--file` option or `--interactive` mode to avoid shell escaping issues.

### Options

```bash
--svg                Generate SVG visualization
--output <filename>  Specify SVG output filename
--ast                Show CommonAST output
--quiet              Suppress informational messages
--help               Show help
```

### Examples

```bash
# Generate SVG visualization
node traceql-tester.js --file examples/basic.traceql --svg

# Show both text and CommonAST output
node traceql-tester.js --file examples/basic.traceql --ast

# Custom SVG filename
node traceql-tester.js --file examples/basic.traceql --svg --output my-query.svg
```

### Interactive Mode

The interactive mode provides a REPL-style interface for testing multiple queries:

```bash
node traceql-tester.js --interactive
```

Available commands in interactive mode:
- `help` - Show available commands
- `svg on/off` - Toggle SVG generation
- `ast on/off` - Toggle CommonAST output
- `examples` - Show example queries
- `exit` or `quit` - Exit the tester

### Use Cases

1. **Valid Query Understanding**: Input a TraceQL query and see its complete parse tree structure
2. **Error Detection**: Invalid queries show error markers (⚠) in the parse tree
3. **SVG Documentation**: Generate professional visualizations for documentation
4. **Learning TraceQL**: Interactive exploration of TraceQL syntax and structure

## Integration with CommonAST

This parser integrates seamlessly with the CommonAST C# project by:
1. Generating AST nodes that match the C# structure
2. Supporting the same node types (Filter, BinaryExpression, Identifier, Literal)
3. Maintaining consistent naming and structure conventions
4. Providing JSON serialization compatible with C# deserialization

## Future Enhancements

- Support for more complex TraceQL constructs
- Integration with the main C# project
- Performance optimizations
- Additional visualization options
