using System.Diagnostics;
using System.Text.Json;

namespace CommonAST
{
    public class TraceQLParserBridge
    {
        public static FilterNode ParseTraceQLQuery(string query)
        {
            // Path to Node script (could be a .js file that calls your parser)
            var scriptPath = Path.GetFullPath("TraceQLParser/dist/index.js");
            
            // Create a Node process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"{scriptPath} \"{query}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            // Parse the JSON output into your C# object
            // TODO: QueryNode.
            return JsonSerializer.Deserialize<FilterNode>(output);
        }
    }
}