namespace CommonAST.Tools;
using System.Text;
using System.Text.Json;

/// <summary>
/// Interactive KQL tester tool providing functionality similar to TraceQL tester
/// </summary>
public static class KqlTester
{
    /// <summary>
    /// Tests a KQL query and displays comprehensive output
    /// </summary>
    /// <param name="query">The KQL query to test</param>
    /// <param name="options">Options for output formatting</param>
    /// <returns>True if query is valid, false otherwise</returns>
    public static bool TestQuery(string query, KqlTestOptions options)
    {
        if (!options.Quiet)
        {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine($"Testing KQL Query: {query}");
            Console.WriteLine("=".PadRight(60, '='));
        }

        try
        {
            // Validate the query first
            var isValid = KqlPrettyPrinter.ValidateQuery(query, out var errors);
            
            if (!options.Quiet)
            {
                Console.WriteLine();
                Console.WriteLine("📋 KQL Parse Tree:");
            }
            
            // Print the parse tree
            var parseTree = KqlPrettyPrinter.PrintKqlParseTree(query);
            Console.WriteLine(parseTree);
            
            if (!isValid)
            {
                Console.WriteLine("❌ Query has syntax errors");
                Console.WriteLine("   Please check the query syntax and try again.");
                return false;
            }
            
            Console.WriteLine("✅ Query compiled successfully!");
            
            // Show CommonAST if requested
            if (options.ShowAst)
            {
                Console.WriteLine();
                Console.WriteLine("🌳 CommonAST Structure:");
                
                var commonAst = KqlPrettyPrinter.ConvertToCommonAst(query);
                if (commonAst != null)
                {
                    if (options.AstAsJson)
                    {
                        var jsonOptions = new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        var json = JsonSerializer.Serialize(commonAst, jsonOptions);
                        Console.WriteLine(json);
                    }
                    else
                    {
                        var astTree = KqlPrettyPrinter.PrintCommonAstTree(commonAst);
                        Console.WriteLine(astTree);
                    }
                }
                else
                {
                    Console.WriteLine("❌ CommonAST conversion failed");
                }
            }
            
            // Generate SVG if requested
            if (options.GenerateSvg)
            {
                var filename = options.OutputFilename ?? $"kql-parse-tree-{DateTime.Now:yyyy-MM-dd-HHmmss}.svg";
                
                if (!options.Quiet)
                {
                    Console.WriteLine();
                    Console.WriteLine($"🎨 Generating SVG visualization: {filename}");
                }
                
                try
                {
                    // This would use the existing Graphviz generation from Program.cs
                    // For now, we'll indicate the feature is available
                    Console.WriteLine($"✅ SVG generation requested. File: {filename}");
                    Console.WriteLine("   (SVG generation will be integrated with existing Graphviz functionality)");
                }
                catch (Exception svgError)
                {
                    Console.WriteLine($"❌ SVG generation failed: {svgError.Message}");
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Query testing failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Runs the interactive KQL tester mode
    /// </summary>
    public static async Task RunInteractiveMode()
    {
        Console.WriteLine(@"
🔍 KQL Interactive Tester
==========================

Enter KQL queries to test them. Type 'help' for commands.
Type 'exit' to quit.

Examples:
  MyTable | where timestamp > ago(1h)
  Events | where Level == ""Error""
  Logs | summarize count() by Level
");

        bool svgMode = false;
        bool astMode = false;
        bool jsonMode = false;
        
        while (true)
        {
            try
            {
                Console.Write("\nKQL> ");
                var input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                    continue;
                
                if (input == "exit" || input == "quit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }
                
                if (input == "help")
                {
                    ShowHelp(svgMode, astMode, jsonMode);
                    continue;
                }
                
                if (input == "examples")
                {
                    ShowExamples();
                    continue;
                }
                
                if (input.StartsWith("svg "))
                {
                    var mode = input.Split(' ')[1];
                    if (mode == "on")
                    {
                        svgMode = true;
                        Console.WriteLine("✅ SVG generation enabled");
                    }
                    else if (mode == "off")
                    {
                        svgMode = false;
                        Console.WriteLine("✅ SVG generation disabled");
                    }
                    else
                    {
                        Console.WriteLine("Usage: svg on/off");
                    }
                    continue;
                }
                
                if (input.StartsWith("ast "))
                {
                    var mode = input.Split(' ')[1];
                    if (mode == "on")
                    {
                        astMode = true;
                        Console.WriteLine("✅ CommonAST output enabled");
                    }
                    else if (mode == "off")
                    {
                        astMode = false;
                        Console.WriteLine("✅ CommonAST output disabled");
                    }
                    else
                    {
                        Console.WriteLine("Usage: ast on/off");
                    }
                    continue;
                }
                
                if (input.StartsWith("json "))
                {
                    var mode = input.Split(' ')[1];
                    if (mode == "on")
                    {
                        jsonMode = true;
                        Console.WriteLine("✅ JSON AST format enabled");
                    }
                    else if (mode == "off")
                    {
                        jsonMode = false;
                        Console.WriteLine("✅ JSON AST format disabled");
                    }
                    else
                    {
                        Console.WriteLine("Usage: json on/off");
                    }
                    continue;
                }
                
                // Test the query
                var options = new KqlTestOptions
                {
                    GenerateSvg = svgMode,
                    ShowAst = astMode,
                    AstAsJson = jsonMode,
                    Quiet = false
                };
                
                TestQuery(input, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Tests a query from a file
    /// </summary>
    /// <param name="filename">Path to the file containing the KQL query</param>
    /// <param name="options">Test options</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool TestQueryFromFile(string filename, KqlTestOptions options)
    {
        try
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"❌ File not found: {filename}");
                return false;
            }
            
            var query = File.ReadAllText(filename).Trim();
            
            if (!options.Quiet)
            {
                Console.WriteLine($"📁 Reading query from: {filename}");
            }
            
            return TestQuery(query, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading file {filename}: {ex.Message}");
            return false;
        }
    }
    
    private static void ShowHelp(bool svgMode, bool astMode, bool jsonMode)
    {
        Console.WriteLine($@"
Available commands:
  help          Show this help
  svg on/off    Toggle SVG generation (currently {(svgMode ? "ON" : "OFF")})
  ast on/off    Toggle CommonAST output (currently {(astMode ? "ON" : "OFF")})
  json on/off   Toggle JSON AST format (currently {(jsonMode ? "ON" : "OFF")})
  examples      Show example queries
  exit/quit     Exit the tester

Or enter any KQL query to test it.
");
    }
    
    private static void ShowExamples()
    {
        Console.WriteLine("\nExample KQL Queries:");
        
        var examples = KqlPrettyPrinter.GetExampleQueries();
        foreach (var example in examples)
        {
            Console.WriteLine($"  {example.Key}:");
            Console.WriteLine($"    {example.Value}");
            Console.WriteLine();
        }
    }
}

/// <summary>
/// Options for KQL testing
/// </summary>
public class KqlTestOptions
{
    /// <summary>
    /// Generate SVG visualization output
    /// </summary>
    public bool GenerateSvg { get; set; } = false;
    
    /// <summary>
    /// Show CommonAST output
    /// </summary>
    public bool ShowAst { get; set; } = false;
    
    /// <summary>
    /// Format AST output as JSON instead of tree
    /// </summary>
    public bool AstAsJson { get; set; } = false;
    
    /// <summary>
    /// Suppress informational output
    /// </summary>
    public bool Quiet { get; set; } = false;
    
    /// <summary>
    /// Output filename for SVG (optional)
    /// </summary>
    public string? OutputFilename { get; set; }
}
