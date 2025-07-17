import { parseTraceQL, printParseTree, logParseTree } from './index';
import { IExpression, Literal, BinaryExpression, Identifier } from './index';
// Import Jest functions directly from @jest/globals to ensure proper typing
import { describe, test, expect, jest } from '@jest/globals';

describe('TraceQL Parser', () => {
  test('should parse empty filter', () => {
    const result = parseTraceQL('{}');
    expect(result.nodeType).toBe('Filter');
    expect(result.expression.nodeType).toBe('Literal');
    
    // Add type assertion to access Literal-specific properties
    const literalExpr = result.expression as Literal;
    expect(literalExpr.valueType).toBe('Boolean');
    expect(literalExpr.value).toBe(true);
  });

  test('should parse simple attribute comparison', () => {
    const result = parseTraceQL('{ span.name = "http.request" }');
    expect(result.nodeType).toBe('Filter');
    expect(result.expression.nodeType).toBe('BinaryExpression');
    
    // Add type assertion to access BinaryExpression-specific properties
    const expr = result.expression as BinaryExpression;
    expect(expr.operator).toBe('=');
    expect(expr.left.nodeType).toBe('Identifier');
    
    // Add type assertion to access Identifier-specific properties
    const leftIdentifier = expr.left as Identifier;
    expect(leftIdentifier.name).toBe('name');
    expect(leftIdentifier.namespace).toBe('span');
    
    expect(expr.right.nodeType).toBe('Literal');
    const rightLiteral = expr.right as Literal;
    expect(rightLiteral.valueType).toBe('String');
    expect(rightLiteral.value).toBe('http.request');
  });

  test('should parse duration comparison', () => {
    const result = parseTraceQL('{ span.duration > 100ms }');
    expect(result.nodeType).toBe('Filter');
    expect(result.expression.nodeType).toBe('BinaryExpression');
    
    // Add type assertion to access BinaryExpression-specific properties
    const expr = result.expression as BinaryExpression;
    expect(expr.operator).toBe('>');
    expect(expr.left.nodeType).toBe('Identifier');
    
    // Add type assertion to access Identifier-specific properties
    const leftIdentifier = expr.left as Identifier;
    expect(leftIdentifier.name).toBe('duration');
    expect(leftIdentifier.namespace).toBe('span');
    
    expect(expr.right.nodeType).toBe('Literal');
    const rightLiteral = expr.right as Literal;
    expect(rightLiteral.valueType).toBe('Duration');
    expect(rightLiteral.value).toBe('100ms');
  });
});

describe('Parse Tree Visualization', () => {
  test('should generate text representation of parse tree', () => {
    const result = printParseTree('{ span.name = "http.request" }');
    expect(result).toContain('SpansetFilter');
    expect(result).toContain('span.name = "http.request"');
    expect(result).toContain('[0-');
  });

  test('should handle empty filter parse tree', () => {
    const result = printParseTree('{}');
    expect(result).toContain('SpansetFilter');
    expect(result).toContain('{}');
  });

  test('should handle invalid query gracefully', () => {
    const result = printParseTree('invalid query syntax');
    expect(result).toContain('⚠'); // Should contain error markers
    expect(result).toContain('invalid');
    expect(result).toContain('query');
    expect(result).toContain('syntax');
  });

  test('should log parse tree without throwing', () => {
    const consoleSpy = jest.spyOn(console, 'log').mockImplementation(() => {});
    
    logParseTree('{ span.name = "test" }');
    
    expect(consoleSpy).toHaveBeenCalledWith('Parse tree for query: { span.name = "test" }');
    expect(consoleSpy).toHaveBeenCalledWith(expect.stringContaining('SpansetFilter'));
    
    consoleSpy.mockRestore();
  });
});
