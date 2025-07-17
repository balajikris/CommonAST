#!/usr/bin/env node

const { printParseTree, saveParseTreeSvg, parseTraceQL } = require('./dist/index.js');
const fs = require('fs');
const path = require('path');

/**
 * TraceQL Tester - Interactive tool for understanding TraceQL queries
 * 
 * Usage:
 *   node traceql-tester.js "{ span.name = \"http.request\" }"
 *   node traceql-tester.js --file query.traceql
 *   node traceql-tester.js --interactive
 */

function printUsage() {
  console.log(`
TraceQL Tester - Interactive tool for understanding TraceQL queries

Usage:
  node traceql-tester.js "query"           Test a TraceQL query string
  node traceql-tester.js --file <file>     Test a query from a file
  node traceql-tester.js --interactive     Interactive mode
  node traceql-tester.js --help            Show this help

Examples:
  node traceql-tester.js "{ span.name = \\"http.request\\" }"
  node traceql-tester.js --file my-query.traceql
  node traceql-tester.js --interactive

Options:
  --svg                Generate SVG output (default: text only)
  --output <filename>  Specify output filename for SVG
  --quiet              Suppress informational output
  --ast                Show CommonAST output
`);
}

function generateTimestamp() {
  return new Date().toISOString().replace(/[:.]/g, '-');
}

async function testQuery(query, options = {}) {
  const { svg = false, output, quiet = false, ast = false } = options;
  
  if (!quiet) {
    console.log('='.repeat(60));
    console.log(`Testing TraceQL Query: ${query}`);
    console.log('='.repeat(60));
  }
  
  try {
    // Test if query compiles
    if (!quiet) {
      console.log('\n📋 Parse Tree (Text Format):');
    }
    const textTree = printParseTree(query);
    console.log(textTree);
    
    // Check for error markers
    const hasErrors = textTree.includes('⚠');
    if (hasErrors) {
      console.log('\n❌ Query has syntax errors (marked with ⚠)');
      console.log('   Please check the query syntax and try again.');
      return false;
    } else {
      console.log('\n✅ Query compiled successfully!');
    }
    
    // Show CommonAST if requested
    if (ast) {
      console.log('\n🌳 CommonAST Structure:');
      try {
        const astResult = parseTraceQL(query);
        console.log(JSON.stringify(astResult, null, 2));
      } catch (astError) {
        console.log(`❌ CommonAST conversion failed: ${astError.message}`);
      }
    }
    
    // Generate SVG if requested
    if (svg) {
      const filename = output || `traceql-parse-tree-${generateTimestamp()}.svg`;
      console.log(`\n🎨 Generating SVG visualization: ${filename}`);
      
      try {
        await saveParseTreeSvg(query, filename);
        console.log(`✅ SVG saved successfully! Open ${filename} in a browser to view.`);
      } catch (svgError) {
        console.log(`❌ SVG generation failed: ${svgError.message}`);
        return false;
      }
    }
    
    return true;
    
  } catch (error) {
    console.log(`\n❌ Query compilation failed: ${error.message}`);
    console.log('   This indicates a syntax error in the TraceQL query.');
    console.log('   Please check the query syntax and try again.');
    return false;
  }
}

async function interactiveMode() {
  const readline = require('readline');
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
  });
  
  console.log(`
🔍 TraceQL Interactive Tester
=============================

Enter TraceQL queries to test them. Type 'help' for commands.
Type 'exit' to quit.

Examples:
  {}
  { span.name = "http.request" }
  { span.duration > 100ms }
  { span.status = "ERROR" }
`);
  
  const askQuestion = (question) => {
    return new Promise((resolve) => {
      rl.question(question, resolve);
    });
  };
  
  let svgMode = false;
  let astMode = false;
  
  while (true) {
    try {
      const input = await askQuestion('\nTraceQL> ');
      const trimmed = input.trim();
      
      if (trimmed === 'exit' || trimmed === 'quit') {
        console.log('Goodbye!');
        break;
      }
      
      if (trimmed === 'help') {
        console.log(`
Available commands:
  help          Show this help
  svg on/off    Toggle SVG generation (currently ${svgMode ? 'ON' : 'OFF'})
  ast on/off    Toggle CommonAST output (currently ${astMode ? 'ON' : 'OFF'})
  examples      Show example queries
  exit/quit     Exit the tester

Or enter any TraceQL query to test it.
`);
        continue;
      }
      
      if (trimmed === 'examples') {
        console.log(`
Example TraceQL Queries:
  {}                                    Empty filter
  { span.name = "http.request" }        Attribute comparison
  { span.duration > 100ms }             Duration comparison
  { span.status = "ERROR" }             Status filter
  { span.name =~ ".*api.*" }            Regular expression
  { .http.method = "GET" }              Attribute without namespace
  { resource.service.name = "web" }     Resource attribute
`);
        continue;
      }
      
      if (trimmed.startsWith('svg ')) {
        const mode = trimmed.split(' ')[1];
        if (mode === 'on') {
          svgMode = true;
          console.log('✅ SVG generation enabled');
        } else if (mode === 'off') {
          svgMode = false;
          console.log('✅ SVG generation disabled');
        } else {
          console.log('Usage: svg on/off');
        }
        continue;
      }
      
      if (trimmed.startsWith('ast ')) {
        const mode = trimmed.split(' ')[1];
        if (mode === 'on') {
          astMode = true;
          console.log('✅ CommonAST output enabled');
        } else if (mode === 'off') {
          astMode = false;
          console.log('✅ CommonAST output disabled');
        } else {
          console.log('Usage: ast on/off');
        }
        continue;
      }
      
      if (trimmed === '') {
        continue;
      }
      
      // Test the query
      await testQuery(trimmed, { 
        svg: svgMode, 
        quiet: false,
        ast: astMode 
      });
      
    } catch (error) {
      console.log(`Error: ${error.message}`);
    }
  }
  
  rl.close();
}

async function main() {
  const args = process.argv.slice(2);
  
  if (args.length === 0 || args.includes('--help')) {
    printUsage();
    return;
  }
  
  if (args.includes('--interactive')) {
    await interactiveMode();
    return;
  }
  
  // Parse command line options
  const options = {
    svg: args.includes('--svg'),
    quiet: args.includes('--quiet'),
    ast: args.includes('--ast')
  };
  
  // Get output filename if specified
  const outputIndex = args.indexOf('--output');
  if (outputIndex !== -1 && outputIndex + 1 < args.length) {
    options.output = args[outputIndex + 1];
  }
  
  // Get query from file or command line
  let query;
  
  if (args.includes('--file')) {
    const fileIndex = args.indexOf('--file');
    if (fileIndex + 1 >= args.length) {
      console.error('Error: --file requires a filename');
      return;
    }
    
    const filename = args[fileIndex + 1];
    try {
      query = fs.readFileSync(filename, 'utf8').trim();
    } catch (error) {
      console.error(`Error reading file ${filename}: ${error.message}`);
      return;
    }
  } else {
    // Find the query string (not an option)
    // Filter out all known options and their values
    const excludedArgs = new Set([
      '--svg', '--quiet', '--ast', '--help', '--interactive', '--file'
    ]);
    
    // Also exclude the output filename if --output is used
    if (outputIndex !== -1 && outputIndex + 1 < args.length) {
      excludedArgs.add(args[outputIndex + 1]);
    }
    
    // Find the first argument that's not an option
    query = args.find(arg => !arg.startsWith('--') && !excludedArgs.has(arg));
  }
  
  if (!query) {
    console.error('Error: No query provided');
    printUsage();
    return;
  }
  
  // Test the query
  const success = await testQuery(query, options);
  process.exit(success ? 0 : 1);
}

// Run the main function
main().catch(error => {
  console.error(`Fatal error: ${error.message}`);
  process.exit(1);
});
