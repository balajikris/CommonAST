I am planning to fork this repo and I want to extend this language to support tracing related constructs. To understand this, I would like to add a new construct that applies KQL's `where` filters to Span sets instead of a trace. I am not interested in adding code generation support, however I would need syntax and semantic analysis done. How should I go about doing this and educate myself in the process? 
Some relevant files and folders I notice are:
1. @microsoft/Kusto-Query-Language/files/grammar - the antlr grammar files reside here.
2.  syntax analysis is here: https://github.com/microsoft/Kusto-Query-Language/tree/master/src/Kusto.Language/Parser
3. semantic analysis is here: https://github.com/microsoft/Kusto-Query-Language/tree/master/src/Kusto.Language/Parser

Great questions! Here’s a structured approach to extend KQL for tracing constructs like Span sets, focusing on syntax and semantic analysis, with guidance for self-education:

---

## 1. **Understand the Existing Architecture**

- **Grammar Files** (`grammar/`):  
  These are ANTLR grammar files defining KQL's syntax. They specify how KQL constructs are parsed into a syntax tree.
- **Syntax Analysis** (`src/Kusto.Language/Parser`):  
  This layer forms a parse tree from input queries using the grammar rules.
- **Semantic Analysis** (same folder):  
  Converts the syntax tree into a semantic tree, checking meaning, types, and more.

**Tip:**  
Read the [README](https://github.com/microsoft/Kusto-Query-Language) and any docs in the grammar or parser folders if present. They often contain architecture and design details.

---

## 2. **Learn About ANTLR and KQL’s Parsing Model**

Before you extend grammar, study:
- The basics of [ANTLR](https://www.antlr.org/) (used for grammar files).
- How a grammar change propagates into C# parser code.
- How semantic analysis works in this repo: especially how expressions like `where` are handled.

**Resources:**  
- [KQL official docs](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/) to understand query constructs.
- General ANTLR tutorials, e.g., [Getting Started with ANTLR](https://github.com/antlr/antlr4/blob/master/doc/getting-started.md).

---

## 3. **Investigate How 'where' Works**

Examine how the existing `where` statement is defined and handled:
- Find grammar rules for `where` in `grammar/Kusto.g4`.
- Trace how this rule is implemented in `Parser` classes (C#).
- See how semantic analysis recognizes and interprets filter criteria.

**Goal:**  
Understand how a new context (Span sets) could be integrated, possibly as a variation or extension of `where`.

---

## 4. **Design the New Construct**

Decide:
- Syntax: How will users specify a `where` on Spans?  
  e.g., `spans where <condition>`
- Semantics: What does ‘where’ mean on Span sets vs. traces?  
  Do you need different semantic validations or error messages?

---

## 5. **Implement Your Extension**

1. **Grammar Change**:  
   - In `grammar/Kusto.g4`, add a rule for your new construct.
   - Test parsing with ANTLR tools locally.

2. **Syntax Analysis**:  
   - Update or augment `Parser` logic (likely in C# under `src/Kusto.Language/Parser`) to recognize your new grammar construct.
   - Validate the parse tree structure.

3. **Semantic Analysis**:  
   - Extend semantic validation to support Span-specific filtering.
   - Ensure error messages and tooling support (e.g. IntelliSense) reflect your new construct.

---

## 6. **Test and Validate**

- Write new unit and integration tests in C# for queries involving your construct.
- Use test queries that exercise the syntax and semantics of filtering Spans.

---

## 7. **Educate Yourself Further**

1. **Read Source Code**:  
   - Go through files in the grammar and parser folders.  
   - Look for documentation or comments regarding syntax evolution or extensions.

2. **Experiment**:  
   - Fork the repo and start with small grammar changes.  
   - Use the test suite to ensure changes don’t break existing functionality.

3. **Community Discussion**:  
   - Look for issues, pull requests, or discussions related to parser and semantic changes in the repo for guidance or best practices.

---

## **Summary Steps**

1. Study KQL’s grammar and parser code, focusing on `where`.
2. Learn ANTLR basics and how grammar gets converted to C# parser code.
3. Design the new syntax for Span filtering.
4. Change the grammar, then update syntax/semantic analysis code.
5. Write tests and validate.
6. Read/document your process so you (and others) can maintain your extension.

---

**Would you like help finding actual files and code snippets that define the current `where` construct, or a step-by-step guide for making a grammar change in this repo?**

this is an abstract answer, give me a concrete one by analyzing this codebase

Here’s a concrete, repo-specific path you can follow, using existing patterns in `microsoft/Kusto-Query-Language` as your guide. I’ll assume your “Span set where …” is a *new query operator* that behaves like `where`, but semantically over “span sets” rather than generic rows/traces.

I’ll walk you through:

1. How `where`/`filter` and similar operators are wired end-to-end.
2. How to add a *new* operator (syntax + semantic analysis) with minimal friction.
3. How to adapt that to “Span sets” and how to use the codebase to educate yourself.

---

## 1. Understand how `where` (filter) is actually implemented

### 1.1. Grammar: `whereOperator` rule

In `grammar/Kql.g4` you saw:

```antlr
whereOperator:
    Keyword=(FILTER | WHERE) (Parameters+=strictQueryOperatorParameter)* Predicate=namedExpression;
```

This is the ANTLR grammar that recognizes `where`/`filter` pipes.

In the C# grammar (the hand-written parser used by the main library), the equivalent is in [`QueryGrammar.cs`](https://github.com/microsoft/Kusto-Query-Language/blob/master/src/Kusto.Language/Parser/QueryGrammar.cs) under `Initialize`:

```csharp
var FilterOperator =
    Rule(
        First(
            Token(SyntaxKind.WhereKeyword, CompletionKind.QueryPrefix, CompletionPriority.Top),
            Token(SyntaxKind.FilterKeyword).Hide()),
        QueryParameterList(QueryOperatorParameters.FilterParameters, equalsNeeded: true),
        Required(NamedExpression, CreateMissingExpression),
        (keyword, parameters, condition) =>
            (QueryOperator)new FilterOperator(keyword, parameters, condition))
    .WithTag("<filter>");
```

Key observations:

- `FilterOperator` is defined as a **rule** combining:
  - A keyword token (`where` / `filter`),
  - A parameter list (`QueryParameterList(QueryOperatorParameters.FilterParameters, ...)`),
  - A required condition expression.
- It returns a `FilterOperator` **syntax node**, which is a subclass of `QueryOperator`.

Later in `Initialize`, the list of operators a pipe can apply is composed:

```csharp
var PipeExpression =
    ApplyOptional(
        First(
            ExtendOperator,
            FilterOperator,
            ParseOperator,
            ParseWhereOperator,
            ParseKvOperator,
            TakeOperator,
            ...
```

So `FilterOperator` gets plugged into the global operator set there.

> **Lesson:** Your new “SpanWhere” operator will need:
> - A `Rule(...)` block like `FilterOperator`.
> - Inclusion in the pipe operator list.
> - A concrete syntax node type.

### 1.2. Related patterns: `GraphWhereNodes` / `GraphWhereEdges`

Your use case (“where-like filter but over specific structure, e.g., span sets”) is *very close* to how graph queries introduce structural `where`-style clauses.

Look at [`QueryGrammar.cs` around the graph operators](https://github.com/microsoft/Kusto-Query-Language/blob/master/src/Kusto.Language/Parser/QueryGrammar.cs):

```csharp
var GraphWhereNodesOperator =
    Rule(
        Token(SyntaxKind.GraphWhereNodesKeyword, CompletionKind.QueryPrefix),
        Required(Expression, CreateMissingExpression),
        (graphWhereNodesKeyword, expression) =>
            (QueryOperator)new GraphWhereNodesOperator(graphWhereNodesKeyword, expression)
        )
    .WithTag("<graph-where-nodes>");

var GraphWhereEdgesOperator =
    Rule(
        Token(SyntaxKind.GraphWhereEdgesKeyword, CompletionKind.QueryPrefix),
        Required(Expression, CreateMissingExpression),
        (graphWhereEdgesKeyword, expression) =>
            (QueryOperator)new GraphWhereEdgesOperator(graphWhereEdgesKeyword, expression)
        )
    .WithTag("<graph-where-edges>");
```

And in the more general clause context:

```csharp
var WhereClause =
    Rule(
        Token(SyntaxKind.WhereKeyword),
        Required(Expression, CreateMissingExpression),
        (keyword, expression) =>
            new WhereClause(keyword, expression));
```

> **Lesson:** There are multiple *flavors* of `where`:
> - The standard `where`/`filter` pipe operator (`FilterOperator` → `FilterOperator` syntax node).
> - Specialized operators like `GraphWhereNodesOperator`, `GraphWhereEdgesOperator`.
> - Clause-level `WhereClause` (e.g., in `join`).

Your Span-filtering operator will likely look structurally like `GraphWhereNodesOperator`: a specific keyword and an expression, with specialized semantics.

---

## 2. Understand the semantic side: binding and type checks

Syntax is only half the story. Semantic analysis is mostly in `src/Kusto.Language/Binder/Binder_NodeBinder.cs`, via `NodeBinder : SyntaxVisitor<SemanticInfo>`.

Example: look at how `JoinWhereClause` is semantically checked:

```csharp
public override SemanticInfo VisitJoinOperator(JoinOperator node)
{
    ...
    switch (node.Condition)
    {
        case JoinOnClause c:
            ...
            break;
        case JoinWhereClause c:
            _binder.CheckIsExactType(c.Expression, ScalarTypes.Bool, diagnostics);
            break;
        default:
            diagnostics.Add(DiagnosticFacts.GetMissingJoinOnClause().WithLocation(node));
            break;
    }
    ...
}
```

For parse-related operators, there is a pattern too:

```csharp
public override SemanticInfo VisitParseWhereOperator(ParseWhereOperator node)
{
    return ParseVisitCommon(node, node.Expression, node.Patterns, node.Parameters);
}

public override SemanticInfo VisitParseOperator(ParseOperator node)
{
    return ParseVisitCommon(node, node.Expression, node.Patterns, node.Parameters);
}
```

> **Lesson:**  
> - Every syntax node that needs semantic info gets a `VisitXxx` override;
> - Common checks (types, parameter validation, etc.) are factored into helpers like `ParseVisitCommon` and `CheckIsExactType`.

You’ll do something similar for SpanWhere: add a `VisitSpanWhereOperator` that:
- Ensures the predicate expression is of boolean type;
- Validates that the input type is a “Span set” (however that’s modeled in your fork);
- Produces an appropriate output type.

---

## 3. How to introduce a *new span-specific where operator* step-by-step

Let’s walk through concrete modifications you’d make for, say, a `spanwhere` operator that applies a predicate to Span sets.

I’ll call the operator `SpanWhereOperator` and the keyword `spanwhere` for illustration—you can adjust naming.

### 3.1. Define the syntax node (via `SyntaxNodeInfos`)

New nodes are defined declaratively in [`src/Kusto.Language.Generators/SyntaxNodeInfos.cs`](https://github.com/microsoft/Kusto-Query-Language/blob/master/src/Kusto.Language.Generators/SyntaxNodeInfos.cs). Note how `ParseWhereOperator` is declared:

```csharp
new SyntaxNodeInfo
{
    Name = "ParseWhereOperator",
    Doc = "",
    Base = "QueryOperator",
    Sealed = true,
    Kind = "ParseWhereOperator",
    Properties = new []
    {
        new SyntaxNodeProperty { Name = "ParseKeyword", Type = "SyntaxToken", Completion="Keyword" },
        new SyntaxNodeProperty { Name = "Parameters", Type = "SyntaxList<NamedParameter>", Completion="None"},
        new SyntaxNodeProperty { Name = "Expression", Type = "Expression", Completion="Scalar"},
        new SyntaxNodeProperty { Name = "WithKeyword", Type = "SyntaxToken", Completion="Keyword" },
        new SyntaxNodeProperty { Name = "Patterns", Type = "SyntaxList<SyntaxNode>", Completion="Syntax"}
    }
},
```

You’ll add something similar:

```csharp
new SyntaxNodeInfo
{
    Name = "SpanWhereOperator",
    Doc = "Filters span sets using a boolean predicate.",
    Base = "QueryOperator",
    Sealed = true,
    Kind = "SpanWhereOperator",
    Properties = new []
    {
        new SyntaxNodeProperty { Name = "SpanWhereKeyword", Type = "SyntaxToken", Completion="Keyword" },
        new SyntaxNodeProperty { Name = "Expression", Type = "Expression", Completion="Boolean" },
        // Optionally Parameters if you want:
        // new SyntaxNodeProperty { Name = "Parameters", Type = "SyntaxList<NamedParameter>", Completion="None"},
    }
},
```

Then you’d run the syntax generator (in the original repo they use a generator project) to produce the concrete `SpanWhereOperator` class in `Syntax` and update visitors. In your fork you can also pattern after existing generated files if you don’t want to automate immediately, but staying consistent with the generator is better.

### 3.2. Add a new `SyntaxKind`

In [`SyntaxKind.cs`](https://github.com/microsoft/Kusto-Query-Language/blob/master/src/Kusto.Language/Syntax/SyntaxKind.cs), find the section near:

```csharp
GraphMarkComponentsOperator,
GraphWhereNodesOperator,
GraphWhereEdgesOperator,
GraphToTableOperator,
...
ParseOperator,
ParseWhereOperator,
ParseKvWithClause,
ParseKvOperator,
...
```

Add:

```csharp
SpanWhereOperator,
```

Then in [`SyntaxFacts.cs`](https://github.com/microsoft/Kusto-Query-Language/blob/master/src/Kusto.Language/Syntax/SyntaxFacts.cs), in the `static` initialization table where `ParseWhereOperator` is registered:

```csharp
new SyntaxData(SyntaxKind.ParseOperator, "", SyntaxCategory.Node),
new SyntaxData(SyntaxKind.ParseWhereOperator, "", SyntaxCategory.Node),
...
// Add:
new SyntaxData(SyntaxKind.SpanWhereOperator, "", SyntaxCategory.Node),
```

This wires the kind into the central metadata.

### 3.3. Wire the operator in the C# grammar (`QueryGrammar`)

Follow the style of `FilterOperator` or `GraphWhereNodesOperator`. For example, in `QueryGrammar.Initialize`:

```csharp
var SpanWhereOperator =
    Rule(
        Token(SyntaxKind.SpanWhereKeyword, CompletionKind.QueryPrefix),
        Required(Expression, CreateMissingExpression),
        (keyword, expression) =>
            (QueryOperator)new SpanWhereOperator(keyword, expression))
    .WithTag("<span-where>");
```

If you want it as a normal pipe operator (e.g., `| spanwhere <predicate>`), you must then plug this rule into the list of operators in the big `First(...)` that defines pipe operators:

```csharp
var PipeExpression =
    ApplyOptional(
        First(
            ExtendOperator,
            FilterOperator,
            ParseOperator,
            ParseWhereOperator,
            ParseKvOperator,
            TakeOperator,
            ...
            GraphWhereNodesOperator,
            GraphWhereEdgesOperator,
            // add:
            SpanWhereOperator,
            ...
        ),
        ...
    );
```

You’ll also need a token kind and lexer support for `SpanWhereKeyword`:

- In `SyntaxKind` you’ll probably add `SpanWhereKeyword` as a token, or if you go via ANTLR you’d add a new keyword token and map it. You can follow how `GraphWhereNodesKeyword` / `GraphWhereEdgesKeyword` are declared and recognized.

### 3.4. (Optional) Update ANTLR grammar

If you care about the ANTLR side (used by the Java implementation and tools in `grammar/`):

- Add a `SPANWHERE` keyword token to `KqlLexer.g4` (or equivalent).
- Add a `spanWhereOperator` rule or extend `whereOperator` or `afterPipeOperator` rules similarly to how other operators are expressed there.

Use the existing `whereOperator` and `graphWhere...` constructs in `Kql.g4` as patterns.

---

## 4. Add semantic analysis for `SpanWhereOperator`

Now wire your new syntax node into the binder in `Binder_NodeBinder.cs`.

1. Find the `NodeBinder` class:

   ```csharp
   private class NodeBinder : SyntaxVisitor<SemanticInfo>
   {
       ...
       public override SemanticInfo VisitParseWhereOperator(ParseWhereOperator node)
       {
           return ParseVisitCommon(node, node.Expression, node.Patterns, node.Parameters);
       }
       ...
   }
   ```

2. Add a new override:

   ```csharp
   public override SemanticInfo VisitSpanWhereOperator(SpanWhereOperator node)
   {
       // Example: ensure the expression is boolean
       var diagnostics = new DiagnosticList();
       _binder.CheckIsExactType(node.Expression, ScalarTypes.Bool, diagnostics);

       // TODO: check input is a span set type, and compute output type
       var inputType = _binder.GetCurrentRowScope().Type; // pattern after other operators
       var resultType = inputType; // if you preserve the same schema

       return new SemanticInfo(resultType, diagnostics);
   }
   ```

3. Optionally, add more span-specific validation:
   - Ensure the predicate only references allowed span columns.
   - Enforce that the operator appears only after a “span-producing” operator.

To learn how to do that, inspect how graph operators do it. For example, around `VisitGraphWhereNodesOperator` / `VisitGraphWhereEdgesOperator` in `Binder_NodeBinder.cs` (pattern to copy: they ensure correct input type and that the predicate is boolean).

---

## 5. Use existing patterns to educate yourself

Here is how I’d use this repo to teach myself the full lifecycle of a new construct, in a very targeted way:

1. **Pick one existing operator that looks like what you want**  
   `GraphWhereNodesOperator` and `GraphWhereEdgesOperator` are close analogs.

2. **Trace it end-to-end:**

   - `SyntaxKind` entry (`GraphWhereNodesOperator`, `GraphWhereNodesKeyword`).
   - `SyntaxFacts` entry for the node.
   - `SyntaxNodeInfos` entry defining its properties.
   - `QueryGrammar` rule constructing the syntax node.
   - `QueryParser` helper (for some operators, the generated parser uses custom methods like `ParseGraphWhereEdgesOperator`; you saw an example for `GraphWhereEdgesOperator`).
   - `Binder_NodeBinder.VisitGraphWhereNodesOperator` for semantics.
   - `QueryOperatorParameters` if it has parameters.

   Read these files for that *one* operator, line-by-line, until you’re comfortable.

3. **Clone the pattern:**

   - Implement `SpanWhereOperator` by literally copying `GraphWhereNodesOperator` and doing a careful rename, changing behavior only where semantics differ.
   - Keep your changes minimal and incremental: first get the syntax recognized; only then layer semantics.

4. **Add tests:**

   Look at existing tests for graph and filter operators. Add:

   - Parse tests: does `T | spanwhere SomeColumn > 0` produce a `SpanWhereOperator` node with the right children?
   - Semantic tests: when the input “table” is a span set, does your binder produce the expected type or diagnostics?

---

## 6. How to approach your tracing/Span design specifically

You said:

> “new construct that applies KQL’s `where` filters to Span sets instead of a trace.”

Concretely, you need to answer:

- **What is a “Span set” in this syntax tree / binder world?**
  - New `TableSymbol` with specific columns (`SpanId`, `TraceId`, `StartTime`, `EndTime`, …)?
  - A new `TypeSymbol`?
- **How does a user obtain a Span set before calling your operator?**
  - Another operator (e.g., `makespans`, `tracespan`, `fromtrace`), or a dedicated table?

Where this flows into the code:

- In your binder, you’ll check the **row scope** type when visiting `SpanWhereOperator`:
  - If it’s not a known “Span set” shape, issue a diagnostic.
- The predicate itself is just a standard KQL expression; use the same checks as `FilterOperator` (boolean expression, accessible columns).

You can inspect how graph operators treat their “graph input” vs standard tables; your span design can mirror that relationship.

---

## 7. Minimal concrete checklist for your fork

If you want a very practical “to-do” list:

1. **Understand by reading**:
   - `QueryGrammar.cs`: `FilterOperator`, `GraphWhereNodesOperator`, `GraphWhereEdgesOperator`, `WhereClause`, `JoinWhereClause`.
   - `Binder_NodeBinder.cs`: `VisitJoinOperator` (look at `JoinWhereClause`), `VisitGraphWhereNodesOperator` / `VisitGraphWhereEdgesOperator`, `VisitFilterOperator` (if present).
   - `SyntaxNodeInfos.cs`: entries for those operators.
   - `SyntaxKind.cs` + `SyntaxFacts.cs`: entries for those operators.

2. **Design your operator**: choose name, keyword, parameters, and what input/output types it expects.

3. **Implement syntax**:
   - Add new entries in `SyntaxKind`, `SyntaxFacts`, `SyntaxNodeInfos`.
   - Add the grammar rule in `QueryGrammar` and plug it into the pipe operator list.
   - (If needed) update ANTLR grammar in `grammar/Kql.g4` and lexer.

4. **Implement semantics**:
   - Add `VisitSpanWhereOperator` in `Binder_NodeBinder`.
   - Use `CheckIsExactType` for the predicate and type-check the input as a Span set.

5. **Add tests** to ensure:
   - Parsing works for your span-where syntax.
   - Semantics behave correctly on valid and invalid inputs.

If you share the specific surface syntax you have in mind for Span sets (e.g. example queries), I can map that directly into the exact spots in `QueryGrammar` and sketch the concrete rule and binder logic for those examples.