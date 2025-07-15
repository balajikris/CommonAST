namespace CommonAST;
using Kusto.Language;
using Kusto.Language.Symbols;
using Kusto.Language.Editor;
using Kusto.Language.Syntax;

public class KQLParse
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CommonAST.exe <KQLQuery> [--output <outputPath>] [--multi]");
            Console.WriteLine("  --multi   Treat input as multiple queries separated by $$ with span filters in []");
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

            case ProjectNode projectNode:
                if (!string.IsNullOrEmpty(projectNode.Keyword))
                    label += $"\\nKeyword: {projectNode.Keyword}";
                label += $"\\nProjections: {projectNode.Projections.Count}";
                break;

            case ProjectionExpression projExpr:
                if (!string.IsNullOrEmpty(projExpr.Alias))
                    label += $"\\nAlias: {projExpr.Alias}";
                if (projExpr.GetResultType() != ExpressionType.Unknown)
                    label += $"\\nResultType: {projExpr.GetResultType()}";
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

            case ProjectNode projectNode:
                foreach (var projection in projectNode.Projections)
                    GenerateGraphvizForCommonAST(projection, writer, nodeId);
                break;

            case ProjectionExpression projExpr:
                GenerateGraphvizForCommonAST(projExpr.Expression, writer, nodeId);
                break;
        }
    }

    #endregion
}
