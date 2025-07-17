# TraceQL Tester Troubleshooting Guide

## Command Line Argument Issues

### Problem: Query string not parsed correctly from command line

**Symptoms:**
- Query appears truncated in output
- "No query provided" error with valid input
- Syntax errors with valid queries

**Root Cause:**
Shell-specific quote escaping and argument parsing issues.

### Solutions by Shell

#### ✅ PowerShell (Windows) - RECOMMENDED
```powershell
# Use single quotes - no escaping needed
node traceql-tester.js '{ span.name = "http.request" }'
node traceql-tester.js '{ span.duration > 100ms }'
node traceql-tester.js '{ span.status = "ERROR" }'
```

#### ⚠️ PowerShell (Windows) - AVOID
```powershell
# This fails - PowerShell truncates at escaped quotes
node traceql-tester.js "{ span.name = \"http.request\" }"
```

#### ✅ Command Prompt (Windows)
```cmd
# Use double quotes and escape inner quotes
node traceql-tester.js "{ span.name = \"http.request\" }"
```

#### ✅ Bash/Zsh (Linux/macOS)
```bash
# Either approach works
node traceql-tester.js "{ span.name = \"http.request\" }"
node traceql-tester.js '{ span.name = "http.request" }'
```

### Alternative Solutions

#### 1. Use File Input (Most Reliable)
```bash
# Create a .traceql file with your query
echo '{ span.name = "http.request" }' > my-query.traceql
node traceql-tester.js --file my-query.traceql
```

#### 2. Use Interactive Mode (Easiest)
```bash
# No escaping needed in interactive mode
node traceql-tester.js --interactive
```

Then simply type your query:
```
TraceQL> { span.name = "http.request" }
```

### Fixed Issues

1. **Argument Parsing**: Updated logic to properly identify query strings vs. options
2. **Shell Escaping**: Added comprehensive documentation for different shells
3. **Error Messages**: Better error reporting when query parsing fails

### Example Test Cases

#### Valid Query Tests
```bash
# These should all work
node traceql-tester.js '{ span.name = "http.request" }'
node traceql-tester.js '{ span.duration > 100ms }'
node traceql-tester.js '{}'
node traceql-tester.js --file examples/basic.traceql
```

#### Invalid Query Tests
```bash
# These should show syntax errors (⚠ markers)
node traceql-tester.js '{ span.name = invalid syntax }'
node traceql-tester.js --file examples/invalid.traceql
```

### Best Practices

1. **For Simple Queries**: Use single quotes in PowerShell
2. **For Complex Queries**: Use `--file` option with .traceql files
3. **For Learning**: Use `--interactive` mode
4. **For Automation**: Use `--file` with proper error handling

### Common Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| "No query provided" | Shell ate the quotes | Use single quotes or file input |
| Query truncated | Escaped quotes broke parsing | Use single quotes in PowerShell |
| Syntax errors with valid query | Shell mangled the string | Use file input or interactive mode |

This troubleshooting guide should help users avoid common pitfalls with shell escaping and argument parsing.
