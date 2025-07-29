namespace CommonAST.Tools;
using Kusto.Language;
using Kusto.Language.Symbols;
using Kusto.Language.Syntax;
using System.Text;

/// <summary>
/// Tool to explore Microsoft's KQL parser metadata capabilities for field references
/// </summary>
public static class KqlMetadataExplorer
{
    /// <summary>
    /// Explores all available metadata for field references in KQL queries
    /// </summary>
    public static void ExploreFieldMetadata()
    {
        Console.WriteLine("🔍 KQL Metadata Explorer");
        Console.WriteLine("========================");
        Console.WriteLine();

        var testQueries = GetTestQueries();
        
        foreach (var query in testQueries)
        {
            Console.WriteLine($"Testing Query: {query.Key}");
            Console.WriteLine($"KQL: {query.Value}");
            Console.WriteLine(new string('-', 60));
            
            ExploreQuery(query.Value);
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Deep exploration of a single KQL query
    /// </summary>
    private static void ExploreQuery(string query)
    {
        try
        {
            var code = KustoCode.Parse(query);
            
            Console.WriteLine("📋 Basic Parse Information:");
            Console.WriteLine($"   Kind: {code.Kind}");
            Console.WriteLine($"   Syntax Type: {code.Syntax?.GetType().Name}");
            
            // Check for syntax errors
            var syntaxDiagnostics = code.GetSyntaxDiagnostics();
            Console.WriteLine($"   Syntax Errors: {syntaxDiagnostics.Count}");
            
            // Check for semantic diagnostics (if available)
            try
            {
                // Note: GetSemanticDiagnostics might not be available in this version
                Console.WriteLine($"   Semantic Analysis: Checking...");
            }
            catch
            {
                Console.WriteLine($"   Semantic Analysis: Not available in this API version");
            }
            
            // Explore symbol information
            ExploreSymbols(code);
            
            // Find and explore NameReference nodes
            ExploreNameReferences(code.Syntax);
            
            // Explore other field-related nodes
            ExploreFieldNodes(code.Syntax);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error exploring query: {ex.Message}");
        }
    }

    /// <summary>
    /// Explores symbol table information
    /// </summary>
    private static void ExploreSymbols(KustoCode code)
    {
        Console.WriteLine();
        Console.WriteLine("🎯 Symbol Information:");
        
        try
        {
            // Check if there's a global symbol context
            if (code.Globals != null)
            {
                Console.WriteLine($"   Global Symbols Available: Yes");
                Console.WriteLine($"   Global Type: {code.Globals.GetType().Name}");
                
                // Try to access database symbols
                if (code.Globals.Database != null)
                {
                    Console.WriteLine($"   Database: {code.Globals.Database.Name}");
                    Console.WriteLine($"   Tables Count: {code.Globals.Database.Tables.Count}");
                    
                    // List some tables if available
                    foreach (var table in code.Globals.Database.Tables.Take(3))
                    {
                        Console.WriteLine($"     Table: {table.Name} (Columns: {table.Columns.Count})");
                        
                        // Show first few columns
                        foreach (var column in table.Columns.Take(3))
                        {
                            Console.WriteLine($"       Column: {column.Name} ({column.Type})");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"   Global Symbols Available: No");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error accessing symbols: {ex.Message}");
        }
    }

    /// <summary>
    /// Explores NameReference nodes and their metadata
    /// </summary>
    private static void ExploreNameReferences(SyntaxNode node)
    {
        Console.WriteLine();
        Console.WriteLine("📝 NameReference Analysis:");
        
        var nameReferences = FindNameReferences(node);
        
        if (nameReferences.Count == 0)
        {
            Console.WriteLine("   No NameReference nodes found");
            return;
        }
        
        foreach (var nameRef in nameReferences)
        {
            Console.WriteLine($"   NameReference: {nameRef.Name.SimpleName}");
            
            // Explore all properties of NameReference
            Console.WriteLine($"     Name: {nameRef.Name}");
            Console.WriteLine($"     Name Type: {nameRef.Name.GetType().Name}");
            
            // Check for symbol binding
            if (nameRef.ReferencedSymbol != null)
            {
                Console.WriteLine($"     Referenced Symbol: {nameRef.ReferencedSymbol}");
                Console.WriteLine($"     Symbol Type: {nameRef.ReferencedSymbol.GetType().Name}");
                Console.WriteLine($"     Symbol Kind: {nameRef.ReferencedSymbol.Kind}");
                
                // If it's a column symbol, get more info
                if (nameRef.ReferencedSymbol is ColumnSymbol column)
                {
                    Console.WriteLine($"     Column Type: {column.Type}");
                    Console.WriteLine($"     Column TypeKind: {column.Type.Kind}");
                }
            }
            else
            {
                Console.WriteLine($"     Referenced Symbol: null");
            }
            
            // Check result type
            var resultType = nameRef.ResultType;
            if (resultType != null)
            {
                Console.WriteLine($"     Result Type: {resultType}");
                Console.WriteLine($"     Result TypeKind: {resultType.Kind}");
            }
            else
            {
                Console.WriteLine($"     Result Type: null");
            }
            
            // Check semantic info
            Console.WriteLine($"     Is Constant: {nameRef.IsConstant}");
            
            // Check for syntax errors (use try-catch since API may vary)
            try
            {
                Console.WriteLine($"     Syntax Node Kind: {nameRef.Kind}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     Error info not available: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Explores other field-related syntax nodes
    /// </summary>
    private static void ExploreFieldNodes(SyntaxNode node)
    {
        Console.WriteLine();
        Console.WriteLine("🔗 Other Field-Related Nodes:");
        
        var pathExpressions = FindNodesByType<PathExpression>(node);
        Console.WriteLine($"   PathExpression nodes: {pathExpressions.Count}");
        foreach (var path in pathExpressions)
        {
            Console.WriteLine($"     Path: {path.Selector} (Type: {path.ResultType})");
        }
        
        var memberExpressions = FindNodesByType<ElementExpression>(node);
        Console.WriteLine($"   ElementExpression nodes: {memberExpressions.Count}");
        foreach (var member in memberExpressions)
        {
            Console.WriteLine($"     Element: {member} (Type: {member.ResultType})");
        }
        
        var bracketExpressions = FindNodesByType<BracketedExpression>(node);
        Console.WriteLine($"   BracketedExpression nodes: {bracketExpressions.Count}");
        
        var functionCalls = FindNodesByType<FunctionCallExpression>(node);
        Console.WriteLine($"   FunctionCallExpression nodes: {functionCalls.Count}");
        foreach (var func in functionCalls)
        {
            Console.WriteLine($"     Function: {func.Name} (Result Type: {func.ResultType})");
        }
    }

    /// <summary>
    /// Finds all NameReference nodes in syntax tree
    /// </summary>
    private static List<NameReference> FindNameReferences(SyntaxNode node)
    {
        var references = new List<NameReference>();
        FindNameReferencesRecursive(node, references);
        return references;
    }

    private static void FindNameReferencesRecursive(SyntaxNode node, List<NameReference> references)
    {
        if (node is NameReference nameRef)
        {
            references.Add(nameRef);
        }

        for (int i = 0; i < node.ChildCount; i++)
        {
            if (node.GetChild(i) is SyntaxNode child)
            {
                FindNameReferencesRecursive(child, references);
            }
        }
    }

    /// <summary>
    /// Finds all nodes of a specific type in syntax tree
    /// </summary>
    private static List<T> FindNodesByType<T>(SyntaxNode node) where T : SyntaxNode
    {
        var nodes = new List<T>();
        FindNodesByTypeRecursive(node, nodes);
        return nodes;
    }

    private static void FindNodesByTypeRecursive<T>(SyntaxNode node, List<T> nodes) where T : SyntaxNode
    {
        if (node is T targetNode)
        {
            nodes.Add(targetNode);
        }

        for (int i = 0; i < node.ChildCount; i++)
        {
            if (node.GetChild(i) is SyntaxNode child)
            {
                FindNodesByTypeRecursive(child, nodes);
            }
        }
    }

    /// <summary>
    /// Test queries for exploring different field scenarios
    /// </summary>
    private static Dictionary<string, string> GetTestQueries()
    {
        return new Dictionary<string, string>
        {
            ["Simple Field Reference"] = "MyTable | where ColumnName == \"test\"",
            ["Multiple Fields"] = "Events | where Level == \"Error\" and EventId > 1000",
            ["Aggregation Fields"] = "Logs | summarize count() by Level, Category",
            ["Function with Fields"] = "Data | where strlen(Message) > 10",
            ["Qualified Reference"] = "T | where T.Field > 100",
            ["Complex Expression"] = "Metrics | where Value > avg(Value) and Category != \"test\"",
            ["Time Field"] = "Events | where Timestamp > ago(1h)",
            ["Nested Reference"] = "Logs | where User.Name == \"admin\""
        };
    }

    /// <summary>
    /// Interactive explorer for testing specific queries
    /// </summary>
    public static async Task RunInteractiveExplorer()
    {
        Console.WriteLine(@"
🔍 KQL Metadata Interactive Explorer
====================================

Enter KQL queries to explore their field metadata.
Type 'examples' to run predefined tests.
Type 'exit' to quit.
");

        while (true)
        {
            try
            {
                Console.Write("\nKQL-Explorer> ");
                var input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                    continue;
                
                if (input == "exit" || input == "quit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }
                
                if (input == "examples")
                {
                    ExploreFieldMetadata();
                    continue;
                }
                
                Console.WriteLine($"\nExploring: {input}");
                Console.WriteLine(new string('=', 50));
                ExploreQuery(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
