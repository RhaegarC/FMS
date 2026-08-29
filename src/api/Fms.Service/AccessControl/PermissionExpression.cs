using System.Text;
using System.Text.RegularExpressions;
using Fms.Model.Entities;

namespace Fms.Service.AccessControl;

/// <summary>Thrown when a permission expression is invalid: bad syntax or an
/// attribute other than user.id / user.email / user.role.</summary>
public sealed class PermissionExpressionException(string message) : Exception(message);

/// <summary>
/// Parses and evaluates a restricted SQL-subset predicate over a
/// <see cref="PermissionSubject"/> (feature 05). Supports equality, inequality,
/// <c>LIKE</c> with <c>%</c>/<c>_</c> wildcards, <c>IN</c> lists, and
/// <c>AND</c>/<c>OR</c>/<c>NOT</c> with parentheses. Only <c>user.id</c>,
/// <c>user.email</c>, and <c>user.role</c> may be referenced; the parser
/// rejects any other attribute or identifier (default deny at the boundary).
/// </summary>
public static class PermissionExpression
{
    /// <summary>Parses <paramref name="expression"/> and evaluates it against
    /// <paramref name="subject"/>. Throws <see cref="PermissionExpressionException"/>
    /// for malformed input — callers treat that as deny.</summary>
    public static bool Evaluate(string expression, PermissionSubject subject)
    {
        if (string.IsNullOrEmpty(expression))
        {
            throw new PermissionExpressionException("Permission expression is empty.");
        }

        var tokens = Tokenize(expression);
        var parser = new Parser(tokens);
        var node = parser.ParseOr();
        parser.ExpectEnd();
        return node.Eval(subject);
    }

    // --- Tokenizer -----------------------------------------------------

    private enum TokenKind
    {
        Word, Number, String, // identifiers, numeric literals, '...'
        Eq, Neq, Like, In, And, Or, Not, // operators + boolean keywords
        Dot, LParen, RParen, Comma, End,
    }

    private readonly record struct Token(TokenKind Kind, string Text);

    private static List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < expression.Length)
        {
            var c = expression[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '\'')
            {
                var sb = new StringBuilder();
                i++; // skip opening quote
                var closed = false;
                while (i < expression.Length)
                {
                    if (expression[i] == '\'')
                    {
                        // '' escapes a literal quote, SQL style.
                        if (i + 1 < expression.Length && expression[i + 1] == '\'')
                        {
                            sb.Append('\'');
                            i += 2;
                            continue;
                        }

                        i++;
                        closed = true;
                        break;
                    }

                    sb.Append(expression[i]);
                    i++;
                }

                if (!closed)
                {
                    throw new PermissionExpressionException("Unterminated string literal.");
                }

                tokens.Add(new Token(TokenKind.String, sb.ToString()));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                {
                    i++;
                }

                var word = expression[start..i];
                tokens.Add(word.ToLowerInvariant() switch
                {
                    "and" => new Token(TokenKind.And, word),
                    "or" => new Token(TokenKind.Or, word),
                    "not" => new Token(TokenKind.Not, word),
                    "like" => new Token(TokenKind.Like, word),
                    "in" => new Token(TokenKind.In, word),
                    _ => new Token(TokenKind.Word, word),
                });
                continue;
            }

            if (char.IsDigit(c))
            {
                var start = i;
                while (i < expression.Length && char.IsDigit(expression[i]))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Number, expression[start..i]));
                continue;
            }

            switch (c)
            {
                case '.': tokens.Add(new Token(TokenKind.Dot, ".")); i++; break;
                case '(': tokens.Add(new Token(TokenKind.LParen, "(")); i++; break;
                case ')': tokens.Add(new Token(TokenKind.RParen, ")")); i++; break;
                case ',': tokens.Add(new Token(TokenKind.Comma, ",")); i++; break;
                case '=': tokens.Add(new Token(TokenKind.Eq, "=")); i++; break;
                case '!':
                    if (i + 1 < expression.Length && expression[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenKind.Neq, "!="));
                        i += 2;
                        break;
                    }

                    throw new PermissionExpressionException("Unexpected character '!'.");
                case '<':
                    if (i + 1 < expression.Length && expression[i + 1] == '>')
                    {
                        tokens.Add(new Token(TokenKind.Neq, "<>"));
                        i += 2;
                        break;
                    }

                    throw new PermissionExpressionException("Unexpected character '<'.");
                default:
                    throw new PermissionExpressionException($"Unexpected character '{c}'.");
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty));
        return tokens;
    }

    // --- Recursive-descent parser over the restricted grammar -------------
    // expr     := orExpr
    // orExpr   := andExpr ( OR andExpr )*
    // andExpr  := notExpr ( AND notExpr )*
    // notExpr  := NOT notExpr | primary
    // primary  := '(' expr ')' | comparison
    // comparison := 'user' '.' field op value
    // value    := string | number | '(' string (',' string)* ')'  (IN list)

    private enum Attr { Id, Email, Role }

    private sealed class Parser(List<Token> tokens)
    {
        private int _pos;
        private Token Current => tokens[_pos];

        public Node ParseOr()
        {
            var node = ParseAnd();
            while (Current.Kind == TokenKind.Or)
            {
                Advance();
                node = new LogicNode(node, ParseAnd(), isOr: true);
            }

            return node;
        }

        public Node ParseAnd()
        {
            var node = ParseNot();
            while (Current.Kind == TokenKind.And)
            {
                Advance();
                node = new LogicNode(node, ParseNot(), isOr: false);
            }

            return node;
        }

        public void ExpectEnd()
        {
            if (Current.Kind != TokenKind.End)
            {
                throw Error("Unexpected tokens after the expression.");
            }
        }

        private Node ParseNot()
        {
            if (Current.Kind == TokenKind.Not)
            {
                Advance();
                return new NotNode(ParseNot());
            }

            return ParsePrimary();
        }

        private Node ParsePrimary()
        {
            if (Current.Kind == TokenKind.LParen)
            {
                Advance();
                var inner = ParseOr();
                Expect(TokenKind.RParen, "Expected ')'.");
                return inner;
            }

            return ParseComparison();
        }

        private Node ParseComparison()
        {
            // Attribute: the only identifier allowed is user.<id|email|role>.
            if (Current.Kind != TokenKind.Word ||
                !Current.Text.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                throw Error("Expected 'user.<attr>' (only user.id, user.email, user.role are allowed).");
            }

            Advance();
            Expect(TokenKind.Dot, "Expected '.' after 'user'.");
            if (Current.Kind != TokenKind.Word)
            {
                throw Error("Expected user.id, user.email, or user.role.");
            }

            var attr = Current.Text.ToLowerInvariant() switch
            {
                "id" => Attr.Id,
                "email" => Attr.Email,
                "role" => Attr.Role,
                _ => throw Error($"Unknown attribute 'user.{Current.Text}'. Only id, email, role are allowed."),
            };
            Advance();

            // Operator: =, !=, <>, LIKE, IN.
            var op = Current.Kind switch
            {
                TokenKind.Eq => "eq",
                TokenKind.Neq => "neq",
                TokenKind.Like => "like",
                TokenKind.In => "in",
                _ => throw Error("Expected =, !=, <>, LIKE, or IN."),
            };
            Advance();

            if (op == "in")
            {
                Expect(TokenKind.LParen, "Expected '(' after IN.");
                var values = new List<string> { ParseValue() };
                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    values.Add(ParseValue());
                }

                Expect(TokenKind.RParen, "Expected ')' to close the IN list.");
                return new InNode(attr, values.ToArray());
            }

            var value = Current;
            if (value.Kind is not (TokenKind.String or TokenKind.Number))
            {
                throw Error("Expected a string or number value.");
            }

            Advance();
            return op switch
            {
                "eq" => new CompareNode(attr, negated: false, value.Text),
                "neq" => new CompareNode(attr, negated: true, value.Text),
                "like" => new LikeNode(attr, value.Text),
                _ => throw Error("Unexpected operator."),
            };
        }

        private string ParseValue()
        {
            var t = Current;
            if (t.Kind is not (TokenKind.String or TokenKind.Number))
            {
                throw Error("Expected a value.");
            }

            Advance();
            return t.Text;
        }

        private Token Advance()
        {
            var t = Current;
            if (t.Kind != TokenKind.End)
            {
                _pos++;
            }

            return t;
        }

        private void Expect(TokenKind kind, string message)
        {
            if (Current.Kind != kind)
            {
                throw Error(message);
            }

            Advance();
        }

        private static PermissionExpressionException Error(string message) =>
            new(message);
    }

    // --- AST nodes -------------------------------------------------------

    private abstract class Node
    {
        public abstract bool Eval(PermissionSubject subject);
    }

    /// <summary>Equality/inequality of an attribute against a literal value.</summary>
    private sealed class CompareNode(Attr attr, bool negated, string value) : Node
    {
        public override bool Eval(PermissionSubject subject)
        {
            var matches = attr switch
            {
                Attr.Id => int.TryParse(value, out var n) && n == subject.Id,
                Attr.Email => value == subject.Email,
                Attr.Role => value == subject.Role,
                _ => false,
            };
            return negated ? !matches : matches;
        }
    }

    /// <summary>SQL LIKE against an attribute: <c>%</c> any sequence, <c>_</c> one char.</summary>
    private sealed class LikeNode(Attr attr, string pattern) : Node
    {
        public override bool Eval(PermissionSubject subject)
        {
            var haystack = attr switch
            {
                Attr.Email => subject.Email,
                Attr.Role => subject.Role,
                _ => null, // LIKE is string-only; user.id has no wildcard meaning
            };
            if (haystack is null)
            {
                return false;
            }

            var regex = new Regex(
                "^" + Regex.Escape(pattern).Replace("%", ".*").Replace("_", ".") + "$",
                RegexOptions.CultureInvariant);
            return regex.IsMatch(haystack);
        }
    }

    /// <summary>Membership in an IN list (string or numeric).</summary>
    private sealed class InNode(Attr attr, string[] values) : Node
    {
        public override bool Eval(PermissionSubject subject) => attr switch
        {
            Attr.Email => values.Contains(subject.Email, StringComparer.Ordinal),
            Attr.Role => values.Contains(subject.Role, StringComparer.Ordinal),
            Attr.Id => values.Any(v => int.TryParse(v, out var n) && n == subject.Id),
            _ => false,
        };
    }

    private sealed class LogicNode(Node left, Node right, bool isOr) : Node
    {
        public override bool Eval(PermissionSubject subject) =>
            isOr ? left.Eval(subject) || right.Eval(subject)
                 : left.Eval(subject) && right.Eval(subject);
    }

    private sealed class NotNode(Node inner) : Node
    {
        public override bool Eval(PermissionSubject subject) => !inner.Eval(subject);
    }
}
