
namespace CommonAST;
using Kusto.Language;
using System.Text.RegularExpressions;

#nullable enable

/// <summary>
/// Parser for handling multiple KQL queries separated by $$ with special handling for span filters in angle brackets
/// </summary>
public class MultiQueryParser
{
    /// <summary>
    /// Parses a multi-query input with $$ separators and << >> span filter grouping
    /// </summary>
    /// <param name="input">
    /// Input in the format "Trace filter $$ << Span filter 1 $$ Span filter 2 >>"
    /// $$ separates individual queries, << >> indicates the content contains span filters
    /// </param>
    /// <returns>A QueryNode representing the combined filters</returns>
    public static QueryNode Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or empty");
        }
        
        Console.WriteLine($"Processing input: \'{input}\'");
        
        // Extract trace and span filters
        var filters = ExtractFilters(input);
        
        if (filters.TraceFilter == null && (filters.SpanFilters == null || filters.SpanFilters.Count == 0))
        {
            throw new ArgumentException("No valid filters found in input");
        }
        
        // Create the resulting QueryNode
        var rootNode = AstBuilder.CreateQuery();
        
        // Parse and add trace filter if it exists
        Expression? traceExpression = null;
        if (!string.IsNullOrWhiteSpace(filters.TraceFilter))
        {
            // Clean up the trace filter query
            var cleanTraceFilter = filters.TraceFilter.Trim();
            Console.WriteLine($"Parsing trace filter: \'{cleanTraceFilter}\'");
            
            var traceFilterCode = KustoCode.Parse(cleanTraceFilter);
            // Check if the query was successfully parsed
            var diagnostics = traceFilterCode.GetSyntaxDiagnostics();
            if (diagnostics.Count > 0)
            {
                throw new ArgumentException($"Syntax errors in trace filter \'{cleanTraceFilter}\': {string.Join(", ", diagnostics.Select(d => d.Message))}");
            }
            
            var traceVisitor = new KqlToCommonAstVisitor();
            traceVisitor.Visit(traceFilterCode.Syntax);
            
            // Extract the trace filter expression from the visitor\'s result
            if (traceVisitor.RootNode.Operations.Count > 0 && traceVisitor.RootNode.Operations[0] is FilterNode filterNode)
            {
                traceExpression = filterNode.TraceExpression;
            }
        }
        
        // Parse span filters if they exist
        List<Expression>? spanExpressions = null;
        if (filters.SpanFilters != null && filters.SpanFilters.Count > 0)
        {
            spanExpressions = new List<Expression>();
            
            foreach (var spanFilter in filters.SpanFilters)
            {
                if (string.IsNullOrWhiteSpace(spanFilter)) continue;
                
                // Clean up the span filter query
                var cleanSpanFilter = spanFilter.Trim();
                Console.WriteLine($"Parsing span filter: \'{cleanSpanFilter}\'");
                
                var spanFilterCode = KustoCode.Parse(cleanSpanFilter);
                // Check if the query was successfully parsed
                var diagnostics = spanFilterCode.GetSyntaxDiagnostics();
                if (diagnostics.Count > 0)
                {
                    throw new ArgumentException($"Syntax errors in span filter \'{cleanSpanFilter}\': {string.Join(", ", diagnostics.Select(d => d.Message))}");
                }
                
                var spanVisitor = new KqlToCommonAstVisitor();
                spanVisitor.Visit(spanFilterCode.Syntax);
                
                // Extract the span filter expression from the visitor\'s result
                if (spanVisitor.RootNode.Operations.Count > 0 && 
                    spanVisitor.RootNode.Operations[0] is FilterNode filterNode && 
                    filterNode.TraceExpression != null)
                {
                    spanExpressions.Add(filterNode.TraceExpression);
                }
            }
        }        // Create the appropriate filter based on available expressions
        FilterNode combinedFilter;
        
        if (traceExpression == null && spanExpressions != null && spanExpressions.Count > 0)
        {
            // For span-only filters, use CreateSpanFilter
            combinedFilter = AstBuilder.CreateSpanFilter(
                spanExpressions,
                SpanFilterCombination.Any // Default to 'Any' combination, can be parameterized if needed
            );
        }
        else
        {
            // For trace filters or combined filters, use CreateCombinedFilter
            combinedFilter = AstBuilder.CreateCombinedFilter(
                traceExpression,
                spanExpressions,
                SpanFilterCombination.Any // Default to 'Any' combination, can be parameterized if needed
            );
        }
        
        // Add the filter to the query operations
        rootNode.Operations.Add(combinedFilter);
        
        return rootNode;
    }    /// <summary>
    /// Extract trace filter and span filters from the input string based on simplified assumptions
    /// </summary>
    /// <param name="input">Input string in the format "Trace filter $$ << Span filter 1 $$ Span filter 2 >>" or "<< Span filter 1 $$ Span filter 2 >>"</param>
    /// <remarks>
    /// Assumes:
    /// 1. Span filters are always within angle brackets << >>
    /// 2. If angle brackets appear in the first part, it's a span-only filter input
    /// 3. Otherwise, the first part is the trace filter, and span filters are in the second part
    /// 4. Content after the span filters is ignored
    /// </remarks>
    /// <returns>A tuple containing the trace filter and a list of span filters</returns>
    private static (string? TraceFilter, List<string>? SpanFilters) ExtractFilters(string input)
    {
        // First check if input starts with angle brackets (span-only filters case)
        var spanOnlyMatch = Regex.Match(input.Trim(), @"^\s*<<\s*(.*?)\s*>>");
        if (spanOnlyMatch.Success)
        {
            // This is a span-only filter input
            string spanFilterContent = spanOnlyMatch.Groups[1].Value.Trim();
            
            if (!string.IsNullOrWhiteSpace(spanFilterContent))
            {
                // Split content by $$ and add each as a span filter
                var splitSpanFilters = spanFilterContent.Split(
                    new[] { "$$" }, 
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                );
                
                // Add non-empty filters to the list
                var spanFiltersList = new List<string>();
                foreach (var filter in splitSpanFilters)
                {
                    var trimmedFilter = filter.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedFilter))
                    {
                        spanFiltersList.Add(trimmedFilter);
                    }
                }
                
                // Return null trace filter and span filters
                return (null, spanFiltersList.Count > 0 ? spanFiltersList : null);
            }
            
            return (null, null);
        }
        
        // Standard case: First, split the input by $$ to separate trace filter and span filters part
        var mainParts = input.Split(new[] { "$$" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        
        // If no parts found, return null values
        if (mainParts.Length == 0)
            return (null, null);
        
        // The first part is the trace filter
        string? traceFilter = mainParts[0].Trim();
        if (string.IsNullOrWhiteSpace(traceFilter))
            traceFilter = null;
        
        // Initialize span filters list as null
        List<string>? spanFilters = null;
          // Span filters are at index 1 and within << >>
        if (mainParts.Length > 1)
        {
            // Try to find span filters in the rest of the input (everything after first $$)
            // Join all parts except the trace filter
            string remainingInput = string.Join(" $$ ", mainParts.Skip(1));
            var spanFilterMatch = Regex.Match(remainingInput, @"<<\s*(.*?)\s*>>", RegexOptions.Singleline);
            
            if (spanFilterMatch.Success)
            {
                // Extract content between angle brackets
                string spanFilterContent = spanFilterMatch.Groups[1].Value.Trim();
                
                if (!string.IsNullOrWhiteSpace(spanFilterContent))
                {
                    // Split content by $$ and add each as a span filter
                    var splitSpanFilters = spanFilterContent.Split(
                        new[] { "$$" }, 
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                    );
                    
                    // Add non-empty filters to the list
                    if (splitSpanFilters.Length > 0)
                    {
                        spanFilters = new List<string>();
                        foreach (var filter in splitSpanFilters)
                        {
                            var trimmedFilter = filter.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmedFilter))
                            {
                                spanFilters.Add(trimmedFilter);
                            }
                        }
                    }
                }
            }
        }
        
        return (
            traceFilter?.Trim(),
            spanFilters?.Count > 0 ? spanFilters : null
        );
    }
}