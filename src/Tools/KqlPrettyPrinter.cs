namespace CommonAST.Tools;
using Kusto.Language;
using Kusto.Language.Symbols;
using Kusto.Language.Editor;
using Kusto.Language.Syntax;
using System.Text;

/// <summary>
/// Provides pretty printing functionality for KQL queries and their parse trees
/// </summary>
public static class KqlPrettyPrinter
{
    /// <summary>
    /// Generates a formatted text representation of a KQL parse tree
    /// </summary>
    /// <param name="query">The KQL query to parse and format</param>
    /// <returns>Formatted text representation of the parse tree</returns>
    public static string PrintKqlParseTree(string query)
    {
        try
        {
            var code = KustoCode.Parse(query);
            
            // Check for syntax errors first
            var diagnostics = code.GetSyntaxDiagnostics();
            if (diagnostics.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("KQL Parse Tree (with errors):");
                
                // Generate tree with error markers
                var treeText = ConsoleTreeRenderer.RenderSyntaxTree(code.Syntax, diagnostics);
                sb.AppendLine(treeText);
                
                sb.AppendLine();
                sb.AppendLine("❌ Syntax errors found:");
                foreach (var diagnostic in diagnostics)
                {
                    sb.AppendLine($"   ⚠ {diagnostic.Message} (at position {diagnostic.Start})");
                }
                
                return sb.ToString();
            }
            
            // Generate clean tree for valid queries
            var result = new StringBuilder();
            result.AppendLine("KQL Parse Tree:");
            result.AppendLine(ConsoleTreeRenderer.RenderSyntaxTree(code.Syntax));
            
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"❌ Failed to parse KQL query: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Generates a formatted text representation of a CommonAST tree
    /// </summary>
    /// <param name="ast">The CommonAST QueryNode to format</param>
    /// <returns>Formatted text representation of the CommonAST</returns>
    public static string PrintCommonAstTree(QueryNode ast)
    {
        try
        {
            var result = new StringBuilder();
            result.AppendLine("CommonAST Structure:");
            result.AppendLine(ConsoleTreeRenderer.RenderCommonAst(ast));
            
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"❌ Failed to render CommonAST: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Validates a KQL query and returns any errors found
    /// </summary>
    /// <param name="query">The KQL query to validate</param>
    /// <param name="errors">List of error messages if validation fails</param>
    /// <returns>True if query is valid, false otherwise</returns>
    public static bool ValidateQuery(string query, out List<string> errors)
    {
        errors = new List<string>();
        
        try
        {
            var code = KustoCode.Parse(query);
            var diagnostics = code.GetSyntaxDiagnostics();
            
            foreach (var diagnostic in diagnostics)
            {
                errors.Add($"{diagnostic.Message} (at position {diagnostic.Start})");
            }
            
            return errors.Count == 0;
        }
        catch (Exception ex)
        {
            errors.Add($"Parse exception: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Converts a KQL query to CommonAST and returns the result
    /// </summary>
    /// <param name="query">The KQL query to convert</param>
    /// <returns>CommonAST QueryNode or null if conversion fails</returns>
    public static QueryNode? ConvertToCommonAst(string query)
    {
        try
        {
            var code = KustoCode.Parse(query);
            
            // Check for syntax errors
            var diagnostics = code.GetSyntaxDiagnostics();
            if (diagnostics.Count > 0)
            {
                return null;
            }
            
            // Convert to CommonAST using the existing visitor
            var visitor = new KqlToCommonAstVisitor();
            visitor.Visit(code.Syntax);
            return visitor.RootNode;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Gets example KQL queries for testing and learning
    /// </summary>
    /// <returns>Dictionary of example names and queries</returns>
    public static Dictionary<string, string> GetExampleQueries()
    {
        return new Dictionary<string, string>
        {
            ["Basic Filter"] = "MyTable | where timestamp > ago(1h)",
            ["Multiple Conditions"] = "Events | where Level == \"Error\" and EventId > 1000",
            ["Aggregation"] = "Logs | summarize count() by Level",
            ["Time Range"] = "Traces | where timestamp between(ago(2h) .. ago(1h))",
            ["String Operations"] = "Messages | where Text contains \"error\" or Text startswith \"warn\"",
            ["Numeric Comparison"] = "Metrics | where Value > 100 and Value < 1000",
            ["Function Call"] = "Data | where isnotnull(UserId) and strlen(Name) > 5"
        };
    }
}
