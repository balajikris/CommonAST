import { parser as traceqlParser } from '@grafana/lezer-traceql';
import { Tree, TreeCursor } from '@lezer/common';
import { Graphviz } from '@hpcc-js/wasm';
import * as fs from 'fs';
import * as path from 'path';

// Import the CommonAST types
// In a real implementation, we would properly reference the C# types
// Here we'll just define TypeScript interfaces that match the C# structure
export interface INode {
  nodeType: string;
  location?: SourceLocation;
}

interface SourceLocation {
  start: Position;
  end: Position;
}

interface Position {
  line: number;
  column: number;
}

// Base interface for all expressions
export interface IExpression extends INode {
  // Base expression properties
}

export interface FilterNode extends INode {
  nodeType: "Filter";
  keyword?: string;
  parameters?: any[];
  expression: IExpression;
}

export interface Identifier extends IExpression {
  nodeType: "Identifier";
  name: string;
  namespace?: string;
}

export interface Literal extends IExpression {
  nodeType: "Literal";
  valueType: "String" | "Integer" | "Float" | "Boolean" | "Null" | "Duration";
  value: any;
}

export interface BinaryExpression extends IExpression {
  nodeType: "BinaryExpression";
  operator: string;
  left: IExpression;
  right: IExpression;
}

/**
 * Generates a formatted text representation of the parse tree
 */
export function printParseTree(query: string): string {
  try {
    const tree = traceqlParser.parse(query);
    const cursor = tree.cursor();
    const treeStructure: string[] = [];
    
    function buildTreeStructure(cursor: TreeCursor, depth: number) {
      const nodeName = cursor.name;
      const nodeText = query.substring(cursor.from, cursor.to);
      const position = `[${cursor.from}-${cursor.to}]`;
      
      // Use box-drawing characters for better visual hierarchy
      let prefix = '';
      if (depth === 0) {
        prefix = '';
      } else {
        // Build the prefix with proper vertical lines
        prefix = '│  '.repeat(depth - 1) + '├─ ';
      }
      
      treeStructure.push(`${prefix}${nodeName} ${position}: "${nodeText}"`);
      
      if (cursor.firstChild()) {
        do {
          buildTreeStructure(cursor, depth + 1);
        } while (cursor.nextSibling());
        cursor.parent();
      }
    }
    
    buildTreeStructure(cursor, 0);
    return treeStructure.join('\n');
  } catch (error) {
    throw new Error(`Query compilation failed: ${error}`);
  }
}

/**
 * Logs the parse tree to console for interactive debugging
 */
export function logParseTree(query: string): void {
  try {
    console.log(`Parse tree for query: ${query}`);
    console.log(printParseTree(query));
  } catch (error) {
    console.error(error);
  }
}

/**
 * Parses a TraceQL query and converts it to the CommonAST structure
 */
export function parseTraceQL(query: string): FilterNode {
  const tree = traceqlParser.parse(query);
  console.log(`Parsing query: ${query}`);
  
  return convertTreeToCommonAST(tree, query);
}

/**
 * Converts a lezer-traceql syntax tree to our CommonAST structure
 */
function convertTreeToCommonAST(tree: Tree, sourceText: string): FilterNode {
  const cursor = tree.cursor();
  
  // Navigate to the SpansetFilter node
  let filterExpression: IExpression | null = null;
  
  // Start from root and navigate down: TraceQL -> SpansetPipelineExpression -> SpansetPipeline -> SpansetFilter
  if (cursor.firstChild()) { // SpansetPipelineExpression (first child of TraceQL)
    if (cursor.firstChild()) { // SpansetPipeline
      if (cursor.firstChild()) { // SpansetFilter
        if (cursor.name as string === "SpansetFilter") {
          // Look for FieldExpression inside SpansetFilter
          if (cursor.firstChild()) {
            if (cursor.name as string === "FieldExpression") {
              filterExpression = processFieldExpression(cursor, sourceText);
            }
            cursor.parent();
          }
        }
        cursor.parent();
      }
      cursor.parent();
    }
    cursor.parent();
  }
  
  // If we couldn't find a filter expression, create a default (empty filter case)
  if (!filterExpression) {
    filterExpression = {
      nodeType: "Literal",
      valueType: "Boolean",
      value: true
    } as Literal;
  }
  
  return {
    nodeType: "Filter",
    expression: filterExpression
  };
}

/**
 * Process a FieldExpression node and its children
 */
function processFieldExpression(cursor: TreeCursor, sourceText: string): IExpression {
  console.log(`Processing FieldExpression: ${cursor.name}, text: "${sourceText.substring(cursor.from, cursor.to)}"`);
  
  let leftExpression: IExpression | null = null;
  let operator = "=";
  let rightExpression: IExpression | null = null;
  
  // Navigate through the FieldExpression structure
  if (cursor.firstChild()) {
    // First child should be the left FieldExpression (AttributeField)
    if (cursor.name as string === "FieldExpression") {
      leftExpression = processNestedFieldExpression(cursor, sourceText);
    }
    
    // Move to operator (FieldOp)
    if (cursor.nextSibling() && cursor.name as string === "FieldOp") {
      operator = sourceText.substring(cursor.from, cursor.to);
    }
    
    // Move to right side (FieldExpression with Static)
    if (cursor.nextSibling() && cursor.name as string === "FieldExpression") {
      rightExpression = processNestedFieldExpression(cursor, sourceText);
    }
    
    cursor.parent();
  }
  
  // If we have both left and right expressions, create a binary expression
  if (leftExpression && rightExpression) {
    return {
      nodeType: "BinaryExpression",
      operator: operator,
      left: leftExpression,
      right: rightExpression
    } as BinaryExpression;
  }
  
  // Fallback to a literal
  return {
    nodeType: "Literal",
    valueType: "Boolean",
    value: true
  } as Literal;
}

/**
 * Process a nested FieldExpression (either AttributeField or Static)
 */
function processNestedFieldExpression(cursor: TreeCursor, sourceText: string): IExpression {
  if (cursor.firstChild()) {
    const nodeName = cursor.name as string;
    
    if (nodeName === "AttributeField") {
      // Process AttributeField -> Span + Identifier
      const result = processAttributeField(cursor, sourceText);
      cursor.parent();
      return result;
    } else if (nodeName === "Static") {
      // Process Static -> String/Duration/etc.
      const result = processStatic(cursor, sourceText);
      cursor.parent();
      return result;
    }
    
    cursor.parent();
  }
  
  // Fallback
  return {
    nodeType: "Literal",
    valueType: "String",
    value: sourceText.substring(cursor.from, cursor.to)
  } as Literal;
}

/**
 * Process an AttributeField (like span.name)
 */
function processAttributeField(cursor: TreeCursor, sourceText: string): Identifier {
  let namespace = "";
  let name = "";
  
  if (cursor.firstChild()) {
    // First child should be the namespace (e.g., "Span")
    if (cursor.name as string === "Span") {
      namespace = "span";
    }
    
    // Move to the identifier
    if (cursor.nextSibling() && cursor.name as string === "Identifier") {
      name = sourceText.substring(cursor.from, cursor.to);
    }
    
    cursor.parent();
  }
  
  return {
    nodeType: "Identifier",
    name: name,
    namespace: namespace
  };
}

/**
 * Process a Static value (String, Duration, etc.)
 */
function processStatic(cursor: TreeCursor, sourceText: string): Literal {
  if (cursor.firstChild()) {
    const nodeName = cursor.name as string;
    const nodeText = sourceText.substring(cursor.from, cursor.to);
    
    if (nodeName === "String") {
      cursor.parent();
      return {
        nodeType: "Literal",
        valueType: "String",
        value: nodeText.substring(1, nodeText.length - 1) // Remove quotes
      };
    } else if (nodeName === "Duration") {
      cursor.parent();
      return {
        nodeType: "Literal",
        valueType: "Duration",
        value: nodeText
      };
    } else if (nodeName === "Integer") {
      cursor.parent();
      return {
        nodeType: "Literal",
        valueType: "Integer",
        value: parseInt(nodeText)
      };
    } else if (nodeName === "Float") {
      cursor.parent();
      return {
        nodeType: "Literal",
        valueType: "Float",
        value: parseFloat(nodeText)
      };
    }
    
    cursor.parent();
  }
  
  // Fallback
  return {
    nodeType: "Literal",
    valueType: "String",
    value: sourceText.substring(cursor.from, cursor.to)
  };
}

/**
 * Process a binary expression (like field = value or a > 10)
 */
function processBinaryExpression(cursor: TreeCursor, sourceText: string): BinaryExpression {
  let left: IExpression = { 
    nodeType: "Literal", 
    valueType: "String", 
    value: "" 
  } as Literal;
  
  let right: IExpression = { 
    nodeType: "Literal", 
    valueType: "String", 
    value: "" 
  } as Literal;
  
  let operator = "=";
  
  if (cursor.firstChild()) {
    // Process left side
    if ((cursor.name as string === "AttributeField") || (cursor.name as string === "IntrinsicField")) {
      left = processField(cursor, sourceText);
    }
    
    // Move to operator
    if (cursor.nextSibling() && (cursor.name as string === "ComparisonOp")) {
      operator = sourceText.substring(cursor.from, cursor.to);
    }
    
    // Move to right side
    if (cursor.nextSibling()) {
      if (cursor.name as string === "String") {
        right = {
          nodeType: "Literal",
          valueType: "String",
          value: sourceText.substring(cursor.from + 1, cursor.to - 1) // Strip quotes
        } as Literal;
      } else if (cursor.name as string === "Integer") {
        right = {
          nodeType: "Literal",
          valueType: "Integer",
          value: parseInt(sourceText.substring(cursor.from, cursor.to))
        } as Literal;
      } else if (cursor.name as string === "Float") {
        right = {
          nodeType: "Literal",
          valueType: "Float",
          value: parseFloat(sourceText.substring(cursor.from, cursor.to))
        } as Literal;
      } else if (cursor.name as string === "Duration") {
        right = {
          nodeType: "Literal",
          valueType: "Duration",
          value: sourceText.substring(cursor.from, cursor.to)
        } as Literal;
      }
    }
    
    cursor.parent();
  }
  
  return {
    nodeType: "BinaryExpression",
    operator,
    left,
    right
  };
}

/**
 * Process a field reference (like span.name or duration)
 */
function processField(cursor: TreeCursor, sourceText: string): Identifier {
  let name = "";
  let namespace = undefined;
  
  const fieldText = sourceText.substring(cursor.from, cursor.to);
  
  // Check if it's a namespaced field (like span.name)
  const parts = fieldText.split(".");
  if (parts.length > 1) {
    namespace = parts[0];
    name = parts.slice(1).join(".");
  } else {
    name = fieldText;
  }
  
  return {
    nodeType: "Identifier",
    name,
    namespace
  };
}

/**
 * Generates a Graphviz DOT representation of the parse tree
 */
export function generateParseTreeDot(query: string): string {
  try {
    const tree = traceqlParser.parse(query);
    const cursor = tree.cursor();
    const dotLines: string[] = [];
    let nodeId = 0;
    
    // Start the DOT graph
    dotLines.push('digraph ParseTree {');
    dotLines.push('  rankdir=TB;');
    dotLines.push('  node [shape=box, style=filled, fillcolor=lightblue];');
    dotLines.push('  edge [color=gray];');
    
    function buildDotStructure(cursor: TreeCursor, parentId: number | null = null): number {
      const currentId = nodeId++;
      const nodeName = cursor.name;
      const nodeText = query.substring(cursor.from, cursor.to);
      const position = `[${cursor.from}-${cursor.to}]`;
      
      // Escape quotes, backslashes, and curly braces in the label
      const escapedText = nodeText
        .replace(/\\/g, '\\\\')
        .replace(/"/g, '\\"')
        .replace(/{/g, '\\{')
        .replace(/}/g, '\\}')
        .replace(/\n/g, '\\n');
      
      // Truncate long text to avoid overly wide nodes
      const truncatedText = escapedText.length > 30 ? escapedText.substring(0, 30) + '...' : escapedText;
      
      const label = `${nodeName}\\n${position}\\n${truncatedText}`;
      
      // Add node
      const fillColor = nodeName.includes('⚠') ? 'lightcoral' : 'lightblue';
      dotLines.push(`  node${currentId} [label="${label}", fillcolor="${fillColor}"];`);
      
      // Add edge from parent if exists
      if (parentId !== null) {
        dotLines.push(`  node${parentId} -> node${currentId};`);
      }
      
      // Process children
      if (cursor.firstChild()) {
        do {
          buildDotStructure(cursor, currentId);
        } while (cursor.nextSibling());
        cursor.parent();
      }
      
      return currentId;
    }
    
    buildDotStructure(cursor);
    dotLines.push('}');
    
    return dotLines.join('\n');
  } catch (error) {
    throw new Error(`Query compilation failed: ${error}`);
  }
}

/**
 * Generates an SVG representation of the parse tree
 */
export async function generateParseTreeSvg(query: string): Promise<string> {
  try {
    const dotSource = generateParseTreeDot(query);
    const graphviz = await Graphviz.load();
    const svg = graphviz.dot(dotSource);
    return svg;
  } catch (error) {
    throw new Error(`SVG generation failed: ${error}`);
  }
}

/**
 * Saves the parse tree as an SVG file
 */
export async function saveParseTreeSvg(query: string, filename?: string): Promise<void> {
  try {
    const svg = await generateParseTreeSvg(query);
    
    // Generate filename if not provided
    if (!filename) {
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      filename = `parse-tree-${timestamp}.svg`;
    }
    
    // Ensure the filename has .svg extension
    if (!filename.endsWith('.svg')) {
      filename += '.svg';
    }
    
    // Write to file
    fs.writeFileSync(filename, svg, 'utf8');
    console.log(`Parse tree saved to: ${filename}`);
  } catch (error) {
    throw new Error(`Failed to save SVG file: ${error}`);
  }
}

/**
 * Integration with the Common AST C# library
 * In a real implementation, this would convert to actual C# objects
 * For now, it just returns the JavaScript objects
 */
export function parseTraceQLToCommonAST(query: string): string {
  const ast = parseTraceQL(query);
  return JSON.stringify(ast, null, 2);
}
