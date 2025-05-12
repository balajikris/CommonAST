namespace CommonAST;
using Kusto.Language;
using Kusto.Language.Symbols;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;

/// <summary>
/// Visitor for converting KQL syntax tree to CommonAST
/// </summary>
public class KqlToCommonAstVisitor
{
    private QueryNode _rootNode;
    private Stack<Expression> _expressionStack = new Stack<Expression>();
    private Stack<List<Expression>> _expressionListStack = new Stack<List<Expression>>();

    public QueryNode RootNode => _rootNode;

    public KqlToCommonAstVisitor()
    {
        _rootNode = AstBuilder.CreateQuery();
    }

    public void Visit(SyntaxNode node)
    {
        if (node == null)
            return;

        switch (node.Kind)
        {
            case SyntaxKind.QueryBlock:
                VisitQueryBlock(node as QueryBlock);
                break;
            case SyntaxKind.PipeExpression:
                VisitPipeExpression(node as PipeExpression);
                break;
            case SyntaxKind.NameReference:
                VisitNameReference(node as NameReference);
                break;
            case SyntaxKind.WhereClause:
                VisitWhereClause(node as WhereClause);
                break;
            case SyntaxKind.FilterOperator:
                VisitFilterOperator(node as FilterOperator);
                break;
            // Handle all binary expression types
            case SyntaxKind.EqualExpression:
            case SyntaxKind.NotEqualExpression:
            case SyntaxKind.LessThanExpression:
            case SyntaxKind.LessThanOrEqualExpression:
            case SyntaxKind.GreaterThanExpression:
            case SyntaxKind.GreaterThanOrEqualExpression:
            case SyntaxKind.AndExpression:
            case SyntaxKind.OrExpression:
            case SyntaxKind.AddExpression:
            case SyntaxKind.SubtractExpression:
            case SyntaxKind.MultiplyExpression:
            case SyntaxKind.DivideExpression:
            case SyntaxKind.ModuloExpression:
                VisitBinaryExpression(node as Kusto.Language.Syntax.BinaryExpression);
                break;
            // Handle literal expressions
            case SyntaxKind.StringLiteralExpression:
            case SyntaxKind.LongLiteralExpression:
            case SyntaxKind.IntLiteralExpression:
            case SyntaxKind.RealLiteralExpression:
            case SyntaxKind.BooleanLiteralExpression:
            case SyntaxKind.DateTimeLiteralExpression:
            case SyntaxKind.TimespanLiteralExpression:
            case SyntaxKind.GuidLiteralExpression:
            case SyntaxKind.NullLiteralExpression:
                VisitLiteral(node);
                break;
            case SyntaxKind.ParenthesizedExpression:
                VisitParenthesizedExpression(node as Kusto.Language.Syntax.ParenthesizedExpression);
                break;
            default:
                // other node types are not yet implemented, just default to visiting the children for now
                VisitChildren(node);
                break;
        }
    }

    private void VisitChildren(SyntaxNode node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            if (node.GetChild(i) is SyntaxNode childNode)
            {
                Visit(childNode);
            }
        }
    }

    private void VisitQueryBlock(Kusto.Language.Syntax.QueryBlock node)
    {
        if (node == null) return;

        foreach (var statement in node.Statements)
        {
            Visit(statement);
        }
    }

    private void VisitPipeExpression(Kusto.Language.Syntax.PipeExpression node)
    {
        if (node == null) return;

        // First expression is typically the table reference
        if (node.Expression is NameReference nameRef)
        {
            _rootNode.Source = nameRef.Name.SimpleName;
        }
        else if (node.Expression != null)
        {
            Visit(node.Expression);
        }

        // Visit the query operator
        Visit(node.Operator);
    }

    private void VisitNameReference(Kusto.Language.Syntax.NameReference node)
    {
        if (node == null) return;

        var identifier = AstBuilder.CreateIdentifier(node.Name.SimpleName);
        _expressionStack.Push(identifier);
    }

    private void VisitFilterOperator(FilterOperator node)
    {
        if (node == null) return;

        // The KQL "filter" keyword is equivalent to "where" in semantics
        string keyword = "filter";

        // Visit the predicate expression
        if (node.Condition != null)
        {
            Visit(node.Condition);
            
            if (_expressionStack.Count > 0)
            {
                var filterExpr = _expressionStack.Pop();
                var filterNode = AstBuilder.CreateFilter(filterExpr, keyword);
                _rootNode.Operations.Add(filterNode);
            }
        }
    }

    private void VisitWhereClause(Kusto.Language.Syntax.WhereClause node)
    {
        if (node == null) return;

        // Visit the filter expression
        if (node.Condition != null)
        {
            Visit(node.Condition);

            if (_expressionStack.Count > 0)
            {
                var filterExpr = _expressionStack.Pop();
                var filterNode = AstBuilder.CreateFilter(filterExpr, "where");
                _rootNode.Operations.Add(filterNode);
            }
        }
    }

    private void VisitBinaryExpression(Kusto.Language.Syntax.BinaryExpression node)
    {
        if (node == null) return;

        // Visit the left and right expressions
        if (node.Left != null)
            Visit(node.Left);

        if (node.Right != null)
            Visit(node.Right);

        if (_expressionStack.Count >= 2)
        {
            var right = _expressionStack.Pop();
            var left = _expressionStack.Pop();

            var opKind = MapKqlOperatorToCommonAST(node.Operator.Text);
            var binExpr = AstBuilder.CreateBinaryExpression(left, opKind, right);
            _expressionStack.Push(binExpr);
        }
    }

    private void VisitLiteral(SyntaxNode node)
    {
        if (node == null) return;

        Literal literal;
        var text = node.ToString();

        switch (node.Kind)
        {
            case SyntaxKind.StringLiteralExpression:
                if (text.StartsWith("\"") && text.EndsWith("\""))
                    text = text.Substring(1, text.Length - 2);

                literal = AstBuilder.CreateLiteral(text, LiteralKind.String);
                break;

            case SyntaxKind.LongLiteralExpression:
                if (long.TryParse(text, out long longValue))
                    literal = AstBuilder.CreateLiteral(longValue, LiteralKind.Integer);
                else
                    literal = AstBuilder.CreateLiteral(0L, LiteralKind.Integer);
                break;

            case SyntaxKind.IntLiteralExpression:
                if (int.TryParse(text, out int intValue))
                    literal = AstBuilder.CreateLiteral(intValue, LiteralKind.Integer);
                else
                    literal = AstBuilder.CreateLiteral(0, LiteralKind.Integer);
                break;

            case SyntaxKind.RealLiteralExpression:
                if (double.TryParse(text, out double doubleValue))
                    literal = AstBuilder.CreateLiteral(doubleValue, LiteralKind.Float);
                else
                    literal = AstBuilder.CreateLiteral(0.0, LiteralKind.Float);
                break;

            case SyntaxKind.BooleanLiteralExpression:
                literal = AstBuilder.CreateLiteral(
                    text.ToLower() == "true",
                    LiteralKind.Boolean);
                break;

            case SyntaxKind.DateTimeLiteralExpression:
                literal = AstBuilder.CreateLiteral(text, LiteralKind.DateTime);
                break;

            case SyntaxKind.TimespanLiteralExpression:
                literal = AstBuilder.CreateLiteral(text, LiteralKind.Duration);
                break;

            case SyntaxKind.GuidLiteralExpression:
                literal = AstBuilder.CreateLiteral(text, LiteralKind.Guid);
                break;

            case SyntaxKind.NullLiteralExpression:
                literal = AstBuilder.CreateLiteral(null, LiteralKind.Null);
                break;

            default:
                literal = AstBuilder.CreateLiteral("unknown literal", LiteralKind.String);
                break;
        }

        _expressionStack.Push(literal);
    }

    private void VisitParenthesizedExpression(Kusto.Language.Syntax.ParenthesizedExpression node)
    {
        if (node == null) return;

        if (node.Expression != null)
        {
            Visit(node.Expression);

            if (_expressionStack.Count > 0)
            {
                var expr = _expressionStack.Pop();
                var parenExpr = new CommonAST.ParenthesizedExpression { Expression = expr };
                _expressionStack.Push(parenExpr);
            }
        }
    }

    // private void VisitUnaryExpression(Kusto.Language.Syntax.UnaryExpression node)
    // {
    //     if (node == null) return;

    //     if (node.Operand != null)
    //     {
    //         Visit(node.Operand);

    //         if (_expressionStack.Count > 0)
    //         {
    //             var operand = _expressionStack.Pop();
    //             var unaryExpr = AstBuilder.CreateUnaryExpression(node.Operator.Text, operand);
    //             _expressionStack.Push(unaryExpr);
    //         }
    //     }
    // }

    // private void VisitFunctionCallExpression(Kusto.Language.Syntax.FunctionCallExpression node)
    // {
    //     if (node == null) return;

    //     string functionName = "unknown";
    //     if (node.Name is Kusto.Language.Syntax.NameReference nameRef)
    //     {
    //         functionName = nameRef.Name.Text;
    //     }

    //     var arguments = new List<Expression>();

    //     // Process arguments
    //     if (node.ArgumentList != null)
    //     {
    //         foreach (var arg in node.ArgumentList.Expressions)
    //         {
    //             Visit(arg);
    //             if (_expressionStack.Count > 0)
    //             {
    //                 arguments.Add(_expressionStack.Pop());
    //             }
    //         }
    //     }

    //     // Reverse the arguments since we process them in reverse order
    //     arguments.Reverse();

    //     var callExpr = AstBuilder.CreateCallExpression(functionName, arguments);
    //     _expressionStack.Push(callExpr);
    // }

    // private void VisitInExpression(Kusto.Language.Syntax.InExpression node)
    // {
    //     if (node == null) return;

    //     if (node.Left != null)
    //         Visit(node.Left);

    //     var rightList = new List<Expression>();

    //     if (node.ValueList != null)
    //     {
    //         foreach (var expr in node.ValueList.Expressions)
    //         {
    //             Visit(expr);
    //             if (_expressionStack.Count > 0)
    //             {
    //                 rightList.Add(_expressionStack.Pop());
    //             }
    //         }
    //     }

    //     // Reverse the right list since we process them in reverse order
    //     rightList.Reverse();

    //     if (_expressionStack.Count > 0)
    //     {
    //         var left = _expressionStack.Pop();
    //         var specialOp = AstBuilder.CreateSpecialOperatorExpression(
    //             left, 
    //             node.Operator.Kind == SyntaxKind.InKeyword ? SpecialOperatorKind.In : SpecialOperatorKind.NotIn,
    //             rightList);

    //         _expressionStack.Push(specialOp);
    //     }
    // }

    // Helper method to map KQL operators to CommonAST BinaryOperatorKind
    private BinaryOperatorKind MapKqlOperatorToCommonAST(string op)
    {
        switch (op)
        {
            case "==": return BinaryOperatorKind.Equal;
            case "!=": return BinaryOperatorKind.NotEqual;
            case "<": return BinaryOperatorKind.LessThan;
            case "<=": return BinaryOperatorKind.LessThanOrEqual;
            case ">": return BinaryOperatorKind.GreaterThan;
            case ">=": return BinaryOperatorKind.GreaterThanOrEqual;
            case "+": return BinaryOperatorKind.Add;
            case "-": return BinaryOperatorKind.Subtract;
            case "*": return BinaryOperatorKind.Multiply;
            case "/": return BinaryOperatorKind.Divide;
            case "%": return BinaryOperatorKind.Modulo;
            case "and": return BinaryOperatorKind.And;
            case "or": return BinaryOperatorKind.Or;
            default: throw new NotSupportedException($"Unsupported binary operator: {op}");
        }
    }
}