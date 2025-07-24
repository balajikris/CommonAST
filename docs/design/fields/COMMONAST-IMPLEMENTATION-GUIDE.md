# CommonAST Implementation Guide for TraceQL & KQL

## 🎯 **Executive Summary for CommonAST Design**

Based on parse tree analysis, here's the definitive guide for implementing CommonAST that supports both TraceQL and KQL.

## 🔍 **Field Classification Results**

### **1. Intrinsic Fields (Built-in, Standardized)**

All intrinsic fields are parsed as `IntrinsicField` regardless of qualification:

```typescript
// Unqualified (defaults to span scope)
"duration" → IntrinsicField [2-10]: "duration"
"name" → IntrinsicField [2-6]: "name"  
"status" → IntrinsicField [2-8]: "status"

// Explicitly qualified
"span:duration" → IntrinsicField [2-15]: "span:duration"
"trace:duration" → IntrinsicField [2-16]: "trace:duration"
```

### **2. Attribute Fields (Custom, User-defined)**

All attribute fields are parsed as `AttributeField` with scope + identifier:

```typescript
// Span attributes
"span.http.method" → AttributeField
├─ Span [2-6]: "span"
├─ Identifier [7-18]: "http.method"

// Resource attributes  
"resource.service.name" → AttributeField
├─ Resource [2-10]: "resource"
├─ Identifier [11-23]: "service.name"

// Unqualified attributes
".custom_field" → AttributeField
├─ Identifier [3-15]: "custom_field"
```

## 🏗️ **Recommended CommonAST Structure**

```typescript
interface FieldReference {
  nodeType: "FieldReference";
  
  // Core classification
  fieldType: "Intrinsic" | "Attribute" | "Column";
  
  // Scope resolution
  scope: "span" | "trace" | "resource" | "event" | "link" | "instrumentation" | null;
  
  // Field identification
  fieldName: string;
  isQualified: boolean;
  
  // Source preservation
  originalText: string;
  sourceLanguage: "TraceQL" | "KQL";
}
```

## 📋 **Mapping Examples**

### **TraceQL → CommonAST Examples**

```typescript
// 1. Unqualified intrinsic
TraceQL: { duration > 100ms }
CommonAST: {
  fieldType: "Intrinsic",
  scope: "span",           // Default scope
  fieldName: "duration",
  isQualified: false,
  originalText: "duration"
}

// 2. Qualified intrinsic
TraceQL: { trace:duration > 1s }
CommonAST: {
  fieldType: "Intrinsic", 
  scope: "trace",          // Explicit scope
  fieldName: "duration",
  isQualified: true,
  originalText: "trace:duration"
}

// 3. Qualified attribute
TraceQL: { span.http.method = "GET" }
CommonAST: {
  fieldType: "Attribute",
  scope: "span",
  fieldName: "http.method", // Hierarchical path
  isQualified: true,
  originalText: "span.http.method"
}

// 4. Unqualified attribute  
TraceQL: { .custom_field = "value" }
CommonAST: {
  fieldType: "Attribute",
  scope: null,              // Search all scopes
  fieldName: "custom_field",
  isQualified: false,
  originalText: ".custom_field"
}

// 5. Resource attribute
TraceQL: { resource.service.name = "api" }
CommonAST: {
  fieldType: "Attribute",
  scope: "resource",
  fieldName: "service.name",
  isQualified: true, 
  originalText: "resource.service.name"
}
```

### **KQL → CommonAST Examples**

```typescript
// KQL column reference
KQL: where http_method == "GET"
CommonAST: {
  fieldType: "Column",
  scope: null,              // KQL doesn't have scopes
  fieldName: "http_method",
  isQualified: false,
  originalText: "http_method"
}
```

## 🔄 **Cross-Language Mapping Strategy**

### **TraceQL → KQL Translation**

```typescript
// Flatten hierarchical TraceQL fields to KQL columns
TraceQL: span.http.method     → KQL: span_http_method
TraceQL: resource.service.name → KQL: resource_service_name
TraceQL: .custom_field        → KQL: custom_field
TraceQL: duration             → KQL: span_duration
TraceQL: trace:duration       → KQL: trace_duration
```

### **KQL → TraceQL Translation**

```typescript
// Map KQL columns to appropriate TraceQL scopes
KQL: http_method         → TraceQL: span.http.method (heuristic)
KQL: service_name        → TraceQL: resource.service.name (heuristic)
KQL: span_duration       → TraceQL: span:duration
KQL: trace_duration      → TraceQL: trace:duration
```

## ⚙️ **Implementation Guidelines**

### **1. Scope Resolution Rules**

```typescript
function resolveScope(field: FieldReference): string {
  if (field.fieldType === "Intrinsic") {
    if (field.isQualified) {
      return extractScopeFromQualifiedName(field.originalText);
    } else {
      return "span"; // Default scope for unqualified intrinsics
    }
  }
  
  if (field.fieldType === "Attribute") {
    if (field.scope) {
      return field.scope; // Explicit scope
    } else {
      return null; // Search all scopes
    }
  }
  
  return null; // KQL columns have no scope
}
```

### **2. Field Name Normalization**

```typescript
function normalizeFieldName(field: FieldReference): string {
  if (field.sourceLanguage === "TraceQL") {
    // Preserve hierarchical structure with dots
    return field.fieldName; // "http.method", "service.name"
  } else {
    // KQL uses flat naming with underscores
    return field.fieldName.replace(/\./g, "_"); // "http_method", "service_name"
  }
}
```

### **3. Cross-Language Compatibility**

```typescript
function convertTraceQLToKQL(field: FieldReference): string {
  const scope = field.scope || "span";
  const flatName = field.fieldName.replace(/\./g, "_");
  
  if (field.fieldType === "Intrinsic") {
    return field.isQualified ? 
      `${scope}_${flatName}` : 
      `span_${flatName}`;
  } else {
    return field.scope ? 
      `${scope}_${flatName}` : 
      flatName;
  }
}
```

## 🎯 **Key Insights for Implementation**

### **1. Critical Discovery: Semantic Equivalence**
- `duration` and `span:duration` are **semantically equivalent**
- Both default to span context
- Your CommonAST should normalize them to the same representation

### **2. Scope Hierarchy**
```
Trace Level:     trace:*, traceDuration
├── Span Level:  span:*, duration, name, status (unqualified default here)
    ├── Resource Level: resource.*
    ├── Event Level:    event.*
    ├── Link Level:     link.*
    └── Custom Level:   .* (searches all levels)
```

### **3. Parser Behavior**
- **IntrinsicField**: Always single token, may contain `:` separator
- **AttributeField**: Always has scope token + identifier token(s)
- **Scope precedence**: Explicit > Default > Search

## 🚀 **Implementation Checklist**

- [ ] **Field classification** (Intrinsic vs Attribute vs Column)
- [ ] **Scope resolution** (explicit vs default vs null)
- [ ] **Semantic equivalence** handling (duration = span:duration)
- [ ] **Cross-language mapping** (TraceQL ↔ KQL)
- [ ] **Original syntax preservation** for round-trip conversion
- [ ] **Hierarchical path handling** (http.method vs http_method)
- [ ] **Default scope rules** per language
- [ ] **Error handling** for invalid scope combinations

This guide provides the complete foundation for implementing a CommonAST that seamlessly handles both TraceQL's hierarchical field model and KQL's flat column model.
