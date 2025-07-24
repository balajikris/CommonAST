# TraceQL Field Semantics for CommonAST Design

## 🎯 **Key Findings for CommonAST Architecture**

### **Duration Field Resolution - CRITICAL DISCOVERY**

Your question about `{ duration > 100ms }` vs `{ trace:duration > 100ms }` reveals important semantic differences:

#### **Parse Tree Analysis:**

1. **`{ duration > 100ms }`**:
   ```
   ├─ FieldExpression [2-10]: "duration"
   │  ├─ IntrinsicField [2-10]: "duration"
   ```

2. **`{ span:duration > 100ms }`**:
   ```
   ├─ FieldExpression [2-15]: "span:duration"
   │  ├─ IntrinsicField [2-15]: "span:duration"
   ```

3. **`{ trace:duration > 1s }`**:
   ```
   ├─ FieldExpression [2-16]: "trace:duration"
   │  ├─ IntrinsicField [2-16]: "trace:duration"
   ```

### **💡 Critical Insight: All are IntrinsicField but semantically different**

## 🏗️ **CommonAST Design Implications**

### **1. Field Classification for CommonAST**

```typescript
interface FieldReference {
  nodeType: "FieldReference";
  fieldType: "Intrinsic" | "Attribute";
  scope?: "span" | "trace" | "resource" | "event" | "link" | "instrumentation";
  fieldName: string;
  isQualified: boolean;  // true for "span:duration", false for "duration"
}
```

### **2. Intrinsic Field Categories**

#### **Unqualified Intrinsic Fields** (Default Scope - likely span-level):
- `duration` → IntrinsicField (default span context)
- `name` → IntrinsicField (default span context)
- `status` → IntrinsicField (default span context)
- `kind` → IntrinsicField (default span context)

#### **Explicitly Qualified Intrinsic Fields**:
- `span:duration` → IntrinsicField with explicit span scope
- `span:id`, `span:name`, `span:status` → Span-scoped intrinsics
- `trace:duration`, `trace:id` → Trace-scoped intrinsics

### **3. Attribute Field Categories**

#### **Qualified Attribute Fields**:
```
├─ AttributeField [2-18]: "span.http.method"
│  ├─ Span [2-6]: "span"           // Scope
│  ├─ Identifier [7-18]: "http.method"  // Field path
```

#### **Unqualified Attribute Fields**:
```
├─ AttributeField [2-15]: ".custom_field"
│  ├─ Identifier [3-15]: "custom_field"  // No explicit scope
```

## 🔍 **Semantic Differences: duration vs span:duration vs trace:duration**

### **Key Question Answered:**
**Are `{ duration > 100ms }` and `{ span:duration > 100ms }` equivalent?**

**Answer**: **Probably YES, but with important nuance:**

1. **Parser Treatment**: Both are classified as `IntrinsicField`
2. **Semantic Context**: Unqualified `duration` likely defaults to span context
3. **Explicit vs Implicit**: `span:duration` is explicit, `duration` is implicit span scope

### **For CommonAST Design:**
```typescript
// These should probably resolve to the same semantic meaning:
{ fieldType: "Intrinsic", scope: "span", fieldName: "duration", isQualified: false }  // duration
{ fieldType: "Intrinsic", scope: "span", fieldName: "duration", isQualified: true }   // span:duration

// While this is semantically different:
{ fieldType: "Intrinsic", scope: "trace", fieldName: "duration", isQualified: true }  // trace:duration
```

## 🌉 **TraceQL ↔ KQL Mapping for CommonAST**

### **TraceQL Field Types:**
1. **Intrinsic Fields** (built-in, standardized)
   - Span-scoped: `duration`, `name`, `status`, `span:*`
   - Trace-scoped: `trace:duration`, `trace:id`
   - Unqualified defaults to span context

2. **Attribute Fields** (custom, user-defined)
   - Qualified: `span.field`, `resource.field`
   - Unqualified: `.field` (searches all scopes)

### **KQL Mapping Strategy:**
```typescript
// TraceQL → KQL CommonAST mapping
TraceQL: { span.http.method = "GET" }
CommonAST: {
  fieldType: "Attribute",
  scope: "span", 
  fieldName: "http.method",
  operator: "=",
  value: "GET"
}

KQL: where http_method == "GET"  // Flatten the hierarchy
CommonAST: {
  fieldType: "Column",
  fieldName: "http_method", 
  operator: "==",
  value: "GET"
}
```

## 📋 **CommonAST Field Taxonomy**

### **Recommended CommonAST Structure:**

```typescript
interface FieldReference {
  nodeType: "FieldReference";
  
  // Core classification
  fieldType: "Intrinsic" | "Attribute" | "Column";  // Column for KQL
  
  // Scope information (TraceQL-specific)
  scope?: "span" | "trace" | "resource" | "event" | "link" | "instrumentation";
  
  // Field identification
  fieldName: string;           // "duration", "http.method", "service.name"
  isQualified: boolean;        // true for "span:duration", false for "duration"
  
  // Original syntax preservation
  originalText: string;        // "duration", "span:duration", ".custom_field"
  
  // Language context
  sourceLanguage: "TraceQL" | "KQL";
}
```

### **Resolution Rules:**

1. **TraceQL Unqualified Intrinsics** (`duration`) → Default to span scope
2. **TraceQL Qualified Intrinsics** (`span:duration`) → Use explicit scope
3. **TraceQL Attributes** (`span.field`) → Use namespace scope
4. **TraceQL Unqualified Attributes** (`.field`) → Search all scopes
5. **KQL Columns** → Direct field reference without hierarchical scope

## 🎯 **Action Items for CommonAST Implementation**

1. **Implement scope resolution** for unqualified fields
2. **Create mapping tables** between TraceQL scopes and KQL flat structure
3. **Handle semantic equivalence** between qualified and unqualified forms
4. **Preserve original syntax** for round-trip conversion
5. **Define default scoping rules** for each query language

This analysis gives you the precise semantics needed to build a robust CommonAST that can handle both TraceQL's hierarchical field model and KQL's flat column model.
