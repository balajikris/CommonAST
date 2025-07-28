namespace CommonAST.Tools;
using Kusto.Language.Syntax;
using Kusto.Language.Editor;
using Kusto.Language;
using System.Text;

/// <summary>
/// Provides console-based tree rendering functionality for syntax trees and CommonAST
/// </summary>
public static class ConsoleTreeRenderer
{
    private const string TreeContinue = "├─ ";
    private const string TreeLast = "└─ ";
    private const string TreeIndent = "│  ";
    private const string TreeSpace = "   ";
    
    /// <summary>
    /// Renders a KQL syntax tree as formatted text
    /// </summary>
    /// <param name="node">The root syntax node to render</param>
    /// <param name="diagnostics">Optional syntax diagnostics for error marking</param>
    /// <returns>Formatted text representation of the syntax tree</returns>
    public static string RenderSyntaxTree(SyntaxNode node, IReadOnlyList<Diagnostic>? diagnostics = null)
    {
        var sb = new StringBuilder();
        var errorPositions = new HashSet<int>();
        
        // Collect error positions if diagnostics provided
        if (diagnostics != null)
        {
            foreach (var diagnostic in diagnostics)
            {
                errorPositions.Add(diagnostic.Start);
            }
        }
        
        RenderSyntaxNodeRecursive(node, sb, "", true, errorPositions);
        return sb.ToString();
    }
    
    /// <summary>
    /// Renders a CommonAST node as formatted text
    /// </summary>
    /// <param name="node">The CommonAST node to render</param>
    /// <returns>Formatted text representation of the CommonAST</returns>
    public static string RenderCommonAst(ASTNode node)
    {
        var sb = new StringBuilder();
        RenderCommonAstRecursive(node, sb, "", true);
        return sb.ToString();
    }
    
    private static void RenderSyntaxNodeRecursive(
        SyntaxNode node, 
        StringBuilder sb, 
        string prefix, 
        bool isLast, 
        HashSet<int> errorPositions)
    {
        // Check if this node has errors
        var hasError = errorPositions.Contains(node.TextStart);
        var errorMarker = hasError ? " ⚠" : "";
        
        // Add the current node
        sb.Append(prefix);
        sb.Append(isLast ? TreeLast : TreeContinue);
        sb.Append($"{node.Kind}");
        
        // Add additional info for certain node types
        string additionalInfo = GetSyntaxNodeInfo(node);
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            sb.Append($" ({additionalInfo})");
        }
        
        sb.AppendLine(errorMarker);
        
        // Process children
        var children = GetSyntaxNodeChildren(node);
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var isLastChild = i == children.Count - 1;
            var childPrefix = prefix + (isLast ? TreeSpace : TreeIndent);
            
            RenderSyntaxNodeRecursive(child, sb, childPrefix, isLastChild, errorPositions);
        }
    }
    
    private static void RenderCommonAstRecursive(ASTNode node, StringBuilder sb, string prefix, bool isLast)
    {
        // Add the current node
        sb.Append(prefix);
        sb.Append(isLast ? TreeLast : TreeContinue);
        sb.Append($"{node.NodeKind}");
        
        // Add additional info for certain node types
        string additionalInfo = GetCommonAstNodeInfo(node);
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            sb.Append($" ({additionalInfo})");
        }
        
        sb.AppendLine();
        
        // Process children based on node type
        var children = GetCommonAstChildren(node);
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var isLastChild = i == children.Count - 1;
            var childPrefix = prefix + (isLast ? TreeSpace : TreeIndent);
            
            RenderCommonAstRecursive(child, sb, childPrefix, isLastChild);
        }
    }
    
    private static string GetSyntaxNodeInfo(SyntaxNode node)
    {
        // Extract meaningful information from different node types
        switch (node.Kind.ToString())
        {
            case "NameReference":
            case "NameDeclaration":
            case "TokenName":
                return GetNodeText(node);
                
            case "LiteralExpression":
                return GetNodeText(node);
                
            case "BinaryExpression":
                // Try to get the operator
                var operatorToken = FindOperatorToken(node);
                return operatorToken ?? "binary op";
                
            case "FunctionCallExpression":
                var functionName = GetFunctionName(node);
                return functionName ?? "function";
                
            case "WhereOperator":
                return "where";
                
            case "SummarizeOperator":
                return "summarize";
                
            case "ProjectOperator":
                return "project";
                
            default:
                // For other nodes, try to get meaningful text if it's short
                var text = GetNodeText(node);
                if (!string.IsNullOrEmpty(text) && text.Length <= 20)
                {
                    return text;
                }
                return "";
        }
    }
    
    private static string GetCommonAstNodeInfo(ASTNode node)
    {
        switch (node)
        {
            case CommonAST.QueryNode queryNode:
                return string.IsNullOrEmpty(queryNode.Source) ? "" : $"Source: {queryNode.Source}";
                
            case CommonAST.FilterNode filterNode:
                var info = new List<string>();
                if (!string.IsNullOrEmpty(filterNode.Keyword))
                    info.Add($"Keyword: {filterNode.Keyword}");
                if (filterNode.SpanFilter != null)
                    info.Add($"SpanFilter: {filterNode.SpanFilter.Combination}");
                return string.Join(", ", info);
                
            case CommonAST.Identifier identifier:
                var parts = new List<string> { identifier.Name };
                if (!string.IsNullOrEmpty(identifier.Namespace))
                    parts.Insert(0, identifier.Namespace);
                return string.Join(".", parts);
                
            case CommonAST.Literal literal:
                return $"{literal.LiteralKind}: {literal.Value?.ToString() ?? "null"}";
                
            case CommonAST.BinaryExpression binExpr:
                return binExpr.Operator.ToString();
                
            case CommonAST.UnaryExpression unaryExpr:
                return unaryExpr.Operator;
                
            case CommonAST.CallExpression callExpr:
                return callExpr.Callee.Name;
                
            case CommonAST.SpecialOperatorExpression specOpExpr:
                return specOpExpr.Operator.ToString();
                
            default:
                return "";
        }
    }
    
    private static List<SyntaxNode> GetSyntaxNodeChildren(SyntaxNode node)
    {
        var children = new List<SyntaxNode>();
        
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is SyntaxNode syntaxChild)
            {
                children.Add(syntaxChild);
            }
        }
        
        return children;
    }
    
    private static List<ASTNode> GetCommonAstChildren(ASTNode node)
    {
        var children = new List<ASTNode>();
        
        switch (node)
        {
            case CommonAST.QueryNode queryNode:
                children.AddRange(queryNode.Operations);
                break;
                
            case CommonAST.FilterNode filterNode:
                if (filterNode.TraceExpression != null)
                    children.Add(filterNode.TraceExpression);
                if (filterNode.SpanFilter?.Expressions != null)
                    children.AddRange(filterNode.SpanFilter.Expressions);
                break;
                
            case CommonAST.BinaryExpression binExpr:
                children.Add(binExpr.Left);
                children.Add(binExpr.Right);
                break;
                
            case CommonAST.UnaryExpression unaryExpr:
                children.Add(unaryExpr.Argument);
                break;
                
            case CommonAST.CallExpression callExpr:
                children.Add(callExpr.Callee);
                children.AddRange(callExpr.Arguments);
                break;
                
            case CommonAST.ParenthesizedExpression parenExpr:
                children.Add(parenExpr.Expression);
                break;
                
            case CommonAST.SpecialOperatorExpression specOpExpr:
                children.Add(specOpExpr.Left);
                children.AddRange(specOpExpr.Right);
                break;
        }
        
        return children;
    }
    
    private static string GetNodeText(SyntaxNode node)
    {
        try
        {
            return node.ToString().Trim();
        }
        catch
        {
            return "";
        }
    }
    
    private static string? FindOperatorToken(SyntaxNode node)
    {
        // Look for operator tokens in binary expressions
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child != null)
            {
                var text = child.ToString().Trim();
                if (IsOperator(text))
                {
                    return text;
                }
            }
        }
        return null;
    }
    
    private static string? GetFunctionName(SyntaxNode node)
    {
        // Look for the function name in function call expressions
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is SyntaxNode syntaxChild && syntaxChild.Kind.ToString().Contains("Name"))
            {
                return syntaxChild.ToString().Trim();
            }
        }
        return null;
    }
    
    private static bool IsOperator(string text)
    {
        return text switch
        {
            "==" or "!=" or "<" or "<=" or ">" or ">=" or "+" or "-" or "*" or "/" or "%" 
            or "and" or "or" or "contains" or "startswith" or "endswith" or "between" => true,
            _ => false
        };
    }
}
