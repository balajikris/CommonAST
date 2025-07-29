# Field Reference Analysis Results

## Overview

Based on the investigation using our KQL Metadata Explorer, this document analyzes Microsoft's KQL parser capabilities for field references and provides recommendations for CommonAST field representation.

## Key Findings from Microsoft's Parser

### 1. Limited Metadata Available

**Critical Discovery**: Microsoft's KQL parser provides minimal field metadata when used without a schema context:

```
📝 NameReference Analysis:
   NameReference: ColumnName
     Name: ColumnName
     Name Type: TokenName
     Referenced Symbol: null      ❌ No symbol binding
     Result Type: null           ❌ No type information
     Is Constant: False
```

### 2. Symbol Table Limitations

**Schema Context Missing**: Without a predefined database schema:

```
🎯 Symbol Information:
   Global Symbols Available: Yes
   Global Type: GlobalState
   Database:
   Tables Count: 0              ❌ No table schemas available
```

### 3. Structural Information Available

**What IS Available**:
- ✅ **Field Names**: `node.Name.SimpleName`
- ✅ **Node Types**: Basic syntax node classification
- ✅ **Parse Structure**: Function calls, path expressions
- ✅ **Syntax Validation**: Error detection works

### 4. Advanced Features Detected

**Path Expressions**: Microsoft's parser does recognize qualified references:
```
Testing Query: Qualified Reference
KQL: T | where T.Field > 100
🔗 Other Field-Related Nodes:
   PathExpression nodes: 1
     Path: Field (Type: )        ✅ Recognizes qualified paths
```

## Analysis by Query Type

### Simple Field References
- **Fields Detected**: Table names and column names correctly identified
- **Type Information**: None available without schema
- **Validation**: Only syntax-level validation

### Aggregation Contexts
```kql
Logs | summarize count() by Level, Category
```
- **Function Recognition**: `count` function detected as `FunctionCallExpression`
- **Grouping Fields**: `Level`, `Category` detected as `NameReference`
- **No Aggregate Validation**: No validation that fields are suitable for grouping

### Complex Expressions
```kql
Metrics | where Value > avg(Value) and Category != "test"
```
- **Mixed Context**: Fields and functions correctly distinguished
- **No Type Validation**: No validation that `Value` is numeric for `avg()`

## Implications for CommonAST Design

### 1. Microsoft's Parser Limitations

**Without Schema Context**:
- ❌ No field type information (string, int, datetime, etc.)
- ❌ No field categorization (intrinsic vs attribute vs computed)
- ❌ No validation of field compatibility with functions
- ❌ No semantic analysis beyond syntax

### 2. What We Must Provide

Since Microsoft's parser doesn't provide rich field metadata without schemas, **CommonAST must implement its own field metadata system**:

#### Required Capabilities:
- **Field Type Classification**: Distinguish field types for validation
- **Data Type Support**: Enable type checking for aggregations
- **Namespace Handling**: Support TraceQL's `span.field` syntax
- **Cross-Language Mapping**: Enable TraceQL ↔ KQL translation

## Recommendation: Enhance Existing Identifier

### Decision: Enhance `Identifier` Rather Than Separate `FieldReference`

**Rationale**:
1. **Microsoft's parser provides minimal metadata** - we need our own system anyway
2. **Fields are universal** - used in `where`, `project`, aggregations across all contexts
3. **Single field representation** - simpler, more maintainable
4. **Backwards compatible** - existing code continues to work

### Proposed Enhanced Identifier

```csharp
/// <summary>
/// Enhanced identifier with field metadata for validation and cross-language support
/// </summary>
public class Identifier : Expression
{
    public override NodeKind NodeKind => NodeKind.Identifier;
    
    /// <summary>Core field properties (existing)</summary>
    public required string Name { get; set; }
    public string? Namespace { get; set; }
    
    /// <summary>Enhanced field metadata (new)</summary>
    public FieldType? FieldType { get; set; }
    public DataType? DataType { get; set; }
    public bool? IsRequired { get; set; }
    
    /// <summary>Source context for translation</summary>
    public string? SourceLanguage { get; set; }
}

/// <summary>Field type categorization</summary>
public enum FieldType
{
    Intrinsic,    // Built-in fields (duration, name, etc.)
    Attribute,    // Custom attributes  
    Table,        // Table/source names
    Function,     // Function names
    Unknown       // Unresolved fields
}

/// <summary>Data types for validation</summary>
public enum DataType
{
    String, Integer, Float, Boolean, Duration, 
    DateTime, Array, Object, Unknown
}
```

### Implementation Strategy

#### Phase 1: Enhance Identifier (Immediate)
1. **Add optional metadata properties** to existing `Identifier`
2. **Maintain backwards compatibility** - all new properties nullable
3. **Update builders** to support enhanced creation
4. **Implement validation helpers** for aggregation contexts

#### Phase 2: Semantic Analysis (Future)
1. **Build field type inference** system
2. **Add validation rules** for aggregation compatibility
3. **Implement cross-language mapping** tables
4. **Schema support** for advanced scenarios

### Benefits of This Approach

1. **Single Source of Truth**: All field references use same enhanced `Identifier`
2. **Gradual Enhancement**: Can add metadata incrementally without breaking changes
3. **Universal Application**: Works for `where`, `project`, aggregations, etc.
4. **Microsoft Integration**: Can still leverage Microsoft's syntax parsing
5. **Cross-Language Ready**: Supports TraceQL ↔ KQL translation needs

## Implementation Priority

### High Priority (Phase 1)
- ✅ **Enhance `Identifier`** with optional metadata properties
- ✅ **Update `AstBuilder`** methods for field creation
- ✅ **Basic validation** for aggregation functions

### Medium Priority (Phase 2)  
- **Field type inference** from usage context
- **Cross-language mapping** tables
- **Advanced validation** rules

### Low Priority (Future)
- **Schema integration** with Microsoft's symbol system
- **Advanced semantic analysis**
- **Performance optimizations**

## Conclusion

Microsoft's KQL parser provides excellent syntax parsing but minimal semantic field metadata without predefined schemas. Therefore, **CommonAST must implement its own field metadata system**.

**Recommendation**: **Enhance the existing `Identifier` class** with optional metadata properties rather than creating a separate `FieldReference` class. This approach:

- Leverages Microsoft's parsing capabilities where they excel
- Adds our own metadata system where Microsoft's is limited  
- Maintains simplicity and backwards compatibility
- Supports all required aggregation and cross-language scenarios

The enhanced `Identifier` will serve as the foundation for field validation, type checking, and cross-language translation in the aggregation system.
