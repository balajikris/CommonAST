namespace CommonAST;
using Kusto.Language;
using Kusto.Language.Symbols;
using Kusto.Language.Editor;
using Kusto.Language.Syntax;
using CommonAST.Tools;

public class KQLParse
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        // Check if this is a KQL tester command
        if (args[0] == "kql-test")
        {
            await HandleKqlTestCommand(args.Skip(1).ToArray());
            return;
        }

        // Check if this is a metadata explorer command
        if (args[0] == "kql-explore")
        {
            await HandleKqlExploreCommand(args.Skip(1).ToArray());
            return;
        }

        // First argument is the KQL query
        var query = args[0];

        // Default output path
        var outputPath = "syntax_tree.dot";
        var commonAstOutputPath = "common_ast.dot";

        // Default to standard parsing
        bool useMultiQueryParser = false;

        // Parse additional switches
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputPath = args[i + 1];
                outputPath = Path.ChangeExtension(outputPath, null) + ".dot";
                commonAstOutputPath = Path.ChangeExtension(outputPath, null) + "_common_ast.dot";
                i++; // Skip the next argument as it's the value for --output
            }
            else if (args[i] == "--multi")
            {
                useMultiQueryParser = true;
            }
            else
            {
                Console.WriteLine($"Unknown argument: {args[i]}");
                return;
            }
        }

        QueryNode commonAst;

        if (useMultiQueryParser)
        {
            try
            {
                // Parse the input as multiple queries with $$ separators
                commonAst = MultiQueryParser.Parse(query);
                Console.WriteLine("Parsed and combined multiple KQL queries successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing multi-query input: {ex.Message}");
                return;
            }

            // No need to generate KQL syntax tree for the multi-query case
        }
        else
        {
            // Standard single query parsing
            var code = KustoCode.Parse(query);

            // Check if the query was successfully parsed or has syntax errors
            var diagnostics = code.GetSyntaxDiagnostics();
            if (diagnostics.Count > 0)
            {
                Console.WriteLine("Syntax errors found:");
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine($"- {diagnostic.Message} (at position {diagnostic.Start})");
                }
                return;
            }

            // Generate Graphviz output for the syntax tree
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("digraph syntax_tree {");
                GenerateGraphvizForKQLParseTree(code.Syntax, writer);
                writer.WriteLine("}");
            }
            Console.WriteLine($"Graphviz output saved to {outputPath}");

            // Convert to CommonAST using the KqlToCommonAstVisitor
            var visitor = new KqlToCommonAstVisitor();
            visitor.Visit(code.Syntax);
            commonAst = visitor.RootNode;
            Console.WriteLine("Converted KQL to CommonAST successfully");
        }        // Generate Graphviz output for the CommonAST
        using (var writer = new StreamWriter(commonAstOutputPath))
        {
            writer.WriteLine("digraph common_ast {");
            writer.WriteLine("  node [shape=box, style=filled, fillcolor=lightblue];");
            GenerateGraphvizForCommonAST(commonAst, writer);
            writer.WriteLine("}");
        }
        Console.WriteLine($"CommonAST Graphviz output saved to {commonAstOutputPath}");
    }


    #region Graphviz Generation

    static void GenerateGraphvizForKQLParseTree(SyntaxNode node, StreamWriter writer, string parent = null)
    {
        var nodeId = Guid.NewGuid().ToString();
        writer.WriteLine($"\"{nodeId}\" [label=\"{node.Kind}\"];");

        if (parent != null)
            writer.WriteLine($"\"{parent}\" -> \"{nodeId}\";");

        // Process all child nodes in the syntax tree
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is SyntaxNode childNode)
            {
                GenerateGraphvizForKQLParseTree(childNode, writer, nodeId);
            }
        }
    }

    static void GenerateGraphvizForCommonAST(ASTNode node, StreamWriter writer, string? parent = null)
    {
        if (node == null)
            return;

        var nodeId = Guid.NewGuid().ToString();

        // Create label with node type and additional info depending on node type
        string label = node.NodeKind.ToString();
        switch (node)
        {
            case QueryNode queryNode:
                if (!string.IsNullOrEmpty(queryNode.Source))
                    label += $"\\nSource: {queryNode.Source}";
                break;

            case FilterNode filterNode:
                if (!string.IsNullOrEmpty(filterNode.Keyword))
                    label += $"\\nKeyword: {filterNode.Keyword}";
                if (filterNode.TraceExpression != null)
                    label += $"\\nHasTraceFilter: true";
                if (filterNode.SpanFilter != null)
                    label += $"\\nHasSpanFilter: true\\nSpanCombination: {filterNode.SpanFilter.Combination}";
                break;

            case Identifier identifier:
                label += $"\\nName: {identifier.Name}";
                if (!string.IsNullOrEmpty(identifier.Namespace))
                    label += $"\\nNamespace: {identifier.Namespace}";
                break;

            case Literal literal:
                label += $"\\nKind: {literal.LiteralKind}";
                label += $"\\nValue: {literal.Value?.ToString() ?? "null"}";
                break;

            case BinaryExpression binExpr:
                label += $"\\nOperator: {binExpr.Operator}";
                break;

            case UnaryExpression unaryExpr:
                label += $"\\nOperator: {unaryExpr.Operator}";
                break;

            case CallExpression callExpr:
                label += $"\\nFunction: {callExpr.Callee.Name}";
                break;

            case SpecialOperatorExpression specOpExpr:
                label += $"\\nOperator: {specOpExpr.Operator}";
                break;
        }

        writer.WriteLine($"\"{nodeId}\" [label=\"{label}\"];");

        if (parent != null)
            writer.WriteLine($"\"{parent}\" -> \"{nodeId}\";");

        // Process child nodes based on the node type
        // [BK]: need GetChild(i) API.
        switch (node)
        {
            case QueryNode queryNode:
                foreach (var op in queryNode.Operations)
                    GenerateGraphvizForCommonAST(op, writer, nodeId);
                break;

            case FilterNode filterNode:
                // GenerateGraphvizForCommonAST(filterNode.Expression, writer, nodeId);
                // Process trace-level filter if exists
                if (filterNode.TraceExpression != null)
                {
                    var traceFilterId = Guid.NewGuid().ToString();
                    writer.WriteLine($"\"{traceFilterId}\" [label=\"TraceFilter\", fillcolor=lightgreen];");
                    writer.WriteLine($"\"{nodeId}\" -> \"{traceFilterId}\";");
                    GenerateGraphvizForCommonAST(filterNode.TraceExpression, writer, traceFilterId);
                }

                // Process span-level filters if exists
                if (filterNode.SpanFilter != null && filterNode.SpanFilter.Expressions.Count > 0)
                {
                    var spanFilterId = Guid.NewGuid().ToString();
                    writer.WriteLine($"\"{spanFilterId}\" [label=\"SpanFilter\\nCombination: {filterNode.SpanFilter.Combination}\", fillcolor=lightyellow];");
                    writer.WriteLine($"\"{nodeId}\" -> \"{spanFilterId}\";");

                    foreach (var expr in filterNode.SpanFilter.Expressions)
                    {
                        GenerateGraphvizForCommonAST(expr, writer, spanFilterId);
                    }
                }
                break;

            case BinaryExpression binExpr:
                GenerateGraphvizForCommonAST(binExpr.Left, writer, nodeId);
                GenerateGraphvizForCommonAST(binExpr.Right, writer, nodeId);
                break;

            case UnaryExpression unaryExpr:
                GenerateGraphvizForCommonAST(unaryExpr.Argument, writer, nodeId);
                break;

            case CallExpression callExpr:
                GenerateGraphvizForCommonAST(callExpr.Callee, writer, nodeId);
                foreach (var arg in callExpr.Arguments)
                    GenerateGraphvizForCommonAST(arg, writer, nodeId);
                break;

            case ParenthesizedExpression parenExpr:
                GenerateGraphvizForCommonAST(parenExpr.Expression, writer, nodeId);
                break;

            case SpecialOperatorExpression specOpExpr:
                GenerateGraphvizForCommonAST(specOpExpr.Left, writer, nodeId);
                foreach (var item in specOpExpr.Right)
                    GenerateGraphvizForCommonAST(item, writer, nodeId);
                break;
        }
    }

    #endregion

    #region KQL Tester Integration

    static void ShowUsage()
    {
        Console.WriteLine("CommonAST - Query Language Processing & AST Transformation");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  CommonAST.exe <KQLQuery> [--output <outputPath>] [--multi]");
        Console.WriteLine("  CommonAST.exe kql-test [options] <query|--file|--interactive>");
        Console.WriteLine("  CommonAST.exe kql-explore [--interactive|--examples]");
        Console.WriteLine();
        Console.WriteLine("Standard Mode:");
        Console.WriteLine("  <KQLQuery>        KQL query to parse and convert to CommonAST");
        Console.WriteLine("  --output <path>   Output path for Graphviz files (default: syntax_tree.dot)");
        Console.WriteLine("  --multi           Treat input as multiple queries separated by $$ with span filters in []");
        Console.WriteLine();
        Console.WriteLine("KQL Tester Mode:");
        Console.WriteLine("  kql-test \"query\"        Test a KQL query string");
        Console.WriteLine("  kql-test --file <file>   Test a query from a file");
        Console.WriteLine("  kql-test --interactive   Interactive mode");
        Console.WriteLine();
        Console.WriteLine("KQL Metadata Explorer Mode:");
        Console.WriteLine("  kql-explore --examples       Run predefined field metadata tests");
        Console.WriteLine("  kql-explore --interactive     Interactive field metadata exploration");
        Console.WriteLine("  kql-explore \"query\"           Explore metadata for a specific query");
        Console.WriteLine();
        Console.WriteLine("KQL Tester Options:");
        Console.WriteLine("  --svg                    Generate SVG output");
        Console.WriteLine("  --ast                    Show CommonAST output");
        Console.WriteLine("  --json                   Format AST as JSON");
        Console.WriteLine("  --output <filename>      Specify output filename for SVG");
        Console.WriteLine("  --quiet                  Suppress informational output");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  CommonAST.exe \"MyTable | where x > 10\"");
        Console.WriteLine("  CommonAST.exe kql-test \"Events | where Level == 'Error'\"");
        Console.WriteLine("  CommonAST.exe kql-test --file my-query.kql --svg --ast");
        Console.WriteLine("  CommonAST.exe kql-test --interactive");
        Console.WriteLine("  CommonAST.exe kql-explore --examples");
        Console.WriteLine("  CommonAST.exe kql-explore --interactive");
    }

    static async Task HandleKqlTestCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No arguments provided for kql-test command");
            Console.WriteLine();
            ShowUsage();
            return;
        }

        // Parse KQL test options
        var options = new KqlTestOptions();
        string? query = null;
        string? filename = null;
        bool interactive = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--svg":
                    options.GenerateSvg = true;
                    break;
                case "--ast":
                    options.ShowAst = true;
                    break;
                case "--json":
                    options.AstAsJson = true;
                    break;
                case "--quiet":
                    options.Quiet = true;
                    break;
                case "--interactive":
                    interactive = true;
                    break;
                case "--file":
                    if (i + 1 < args.Length)
                    {
                        filename = args[i + 1];
                        i++; // Skip the filename argument
                    }
                    else
                    {
                        Console.WriteLine("Error: --file requires a filename");
                        return;
                    }
                    break;
                case "--output":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFilename = args[i + 1];
                        i++; // Skip the filename argument
                    }
                    else
                    {
                        Console.WriteLine("Error: --output requires a filename");
                        return;
                    }
                    break;
                default:
                    // If it doesn't start with --, treat it as the query
                    if (!args[i].StartsWith("--"))
                    {
                        query = args[i];
                    }
                    else
                    {
                        Console.WriteLine($"Error: Unknown option: {args[i]}");
                        return;
                    }
                    break;
            }
        }

        // Execute the appropriate mode
        try
        {
            if (interactive)
            {
                await KqlTester.RunInteractiveMode();
            }
            else if (!string.IsNullOrEmpty(filename))
            {
                var success = KqlTester.TestQueryFromFile(filename, options);
                Environment.Exit(success ? 0 : 1);
            }
            else if (!string.IsNullOrEmpty(query))
            {
                var success = KqlTester.TestQuery(query, options);
                Environment.Exit(success ? 0 : 1);
            }
            else
            {
                Console.WriteLine("Error: No query, file, or interactive mode specified");
                ShowUsage();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    #endregion

    #region KQL Metadata Explorer Integration

    static async Task HandleKqlExploreCommand(string[] args)
    {
        try
        {
            if (args.Length == 0 || (args.Length == 1 && args[0] == "--examples"))
            {
                // Run predefined examples
                KqlMetadataExplorer.ExploreFieldMetadata();
                return;
            }

            if (args.Length == 1 && args[0] == "--interactive")
            {
                // Run interactive mode
                await KqlMetadataExplorer.RunInteractiveExplorer();
                return;
            }

            if (args.Length == 1 && !args[0].StartsWith("--"))
            {
                // Explore a specific query
                Console.WriteLine($"🔍 Exploring KQL Query: {args[0]}");
                Console.WriteLine(new string('=', 50));
                
                var code = KustoCode.Parse(args[0]);
                Console.WriteLine("📋 Basic Parse Information:");
                Console.WriteLine($"   Kind: {code.Kind}");
                Console.WriteLine($"   Syntax Type: {code.Syntax?.GetType().Name}");
                
                var syntaxDiagnostics = code.GetSyntaxDiagnostics();
                Console.WriteLine($"   Syntax Errors: {syntaxDiagnostics.Count}");

                if (syntaxDiagnostics.Count > 0)
                {
                    Console.WriteLine("❌ Syntax errors found:");
                    foreach (var diagnostic in syntaxDiagnostics)
                    {
                        Console.WriteLine($"   - {diagnostic.Message} (at position {diagnostic.Start})");
                    }
                    return;
                }

                // Use our explorer to analyze the query
                return;
            }

            Console.WriteLine("Error: Invalid arguments for kql-explore command");
            Console.WriteLine();
            ShowUsage();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error in metadata explorer: {ex.Message}");
            Environment.Exit(1);
        }
    }

    #endregion
}
