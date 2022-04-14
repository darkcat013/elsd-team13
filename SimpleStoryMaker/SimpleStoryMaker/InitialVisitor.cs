using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using SimpleStoryMaker.Content;

namespace SimpleStoryMaker
{
    public class InitialVisitor : StoryBaseVisitor<object?>
    {
        public Dictionary<string, object?> Player { get; set; } = new();
        public StoryParser.StartContext? StartScene { get; set; }
        public List<StoryParser.SceneContext> Scenes { get; set; } = new();
        public StoryParser.EndContext? EndScene { get; set; }
        public List<string> ScenesNames { get; set; } = new();
        public List<string> SemanticErrors { get; private set; } = new();
        public Dictionary<string, object> Variables { get; set; } = new();

        private readonly List<StoryParser.GoToContext> _goTos = new();

        public override object? VisitAttribute(StoryParser.AttributeContext context)
        {
            var key = context.IDENTIFIER().GetText();

            if (Player.ContainsKey(key))
                AddError(context.IDENTIFIER().Symbol, $"Duplicate player attribute: '{key}'");
            else
            {
                Player[key] = Visit(context.expression());
            }
            return base.VisitAttribute(context);
        }
        public override object? VisitStart(StoryParser.StartContext context)
        {
            if (StartScene is null) StartScene = context;
            return base.VisitStart(context);
        }

        public override object? VisitEnd(StoryParser.EndContext context)
        {
            if (EndScene is null) EndScene = context;
            AddErrorsForGoTos();
            return base.VisitEnd(context);
        }

        public override object? VisitScene(StoryParser.SceneContext context)
        {
            var sceneName = context.IDENTIFIER().GetText();

            if (ScenesNames.Contains(sceneName))
                AddError(context.IDENTIFIER().Symbol, $"Duplicate scene name: '{sceneName}'");

            ScenesNames.Add(sceneName);
            Scenes.Add(context);
            return base.VisitScene(context);
        }

        public override object? VisitChoices(StoryParser.ChoicesContext context)
        {
            var choices = context.choice();

            var choicesString = choices.Select(x => x.IDENTIFIER().GetText()).ToList() as List<string?>;
            var distinct = new HashSet<string?>(choicesString);
            if (choicesString.Count != distinct.Count)
            {
                IToken? symbol = default;
                if (context.Parent is StoryParser.StartContext stc)
                    symbol = stc.START().Symbol;
                else if (context.Parent is StoryParser.EndContext ec)
                    symbol = ec.END().Symbol;
                else if (context.Parent is StoryParser.SceneContext sc)
                    symbol = sc.IDENTIFIER().Symbol;

                AddError(symbol, $"Duplicate choice option in scene: '{symbol?.Text}'");
            }

            return base.VisitChoices(context);
        }

        public override object? VisitGoTo(StoryParser.GoToContext context)
        {
            if (context.IDENTIFIER() is { } id)
            {
                var sceneName = id.GetText();
                if (!ScenesNames.Contains(sceneName))
                    _goTos.Add(context);
            }

            return base.VisitGoTo(context);
        }

        public override object? VisitLiteral(StoryParser.LiteralContext context)
        {
            if (context.number() is { } d)
                return double.Parse(d.GetText());
            if (context.STRING() is { } s)
                return s.GetText()[1..^1];
            if (context.BOOL() is { } b)
                return b.GetText() == "true";

            throw new NotImplementedException();
        }

        public override object? VisitMultiplicativeExpression(StoryParser.MultiplicativeExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));
            var op = context.MULTOP().GetText();

            if (!(left is double && right is double))
                switch (op)
                {
                    case "*": AddError(context.expression(0).Start, $"Cannot multiply values of types '{left?.GetType()}' and '{right?.GetType()}'"); break;
                    case "/": AddError(context.expression(0).Start, $"Cannot divide values of types '{left?.GetType()}' and '{right?.GetType()}'"); break;
                    default:
                        break;
                }
            else
            {
                return op switch
                {
                    "*" => Multiply(left, right),
                    "/" => Divide(left, right),
                    _ => throw new NotImplementedException()
                };
            }

            return base.VisitMultiplicativeExpression(context);
        }


        public override object? VisitParanthesizedExpression(StoryParser.ParanthesizedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override object? VisitAdditiveExpression(StoryParser.AdditiveExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));
            var op = context.ADDOP().GetText();
            if (!(left is string || right is string) && !(left is double && right is double))
                switch (op)
                {
                    case "+": AddError(context.expression(0).Start, $"Cannot add values of types '{left?.GetType()}' and '{right?.GetType()}'"); break;
                    case "-": AddError(context.expression(0).Start, $"Cannot subtract values of types '{left?.GetType()}' and '{right?.GetType()}'"); break;
                    default:
                        break;
                }
            else
            {
                return op switch
                {
                    "+" => Add(left, right),
                    "-" => Subtract(left, right),
                    _ => throw new NotImplementedException()
                };
            }

            return base.VisitAdditiveExpression(context);
        }

        public override object? VisitComparisonExpression(StoryParser.ComparisonExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            var op = context.COMPAREOP().GetText();

            var sameType = left?.GetType() == right?.GetType();
            if (!sameType)
            {
                AddError(context.expression(0).Start, $"Cannot compare values of types '{left?.GetType()}' and '{right?.GetType()}'");
            }
            else if (left is bool && !(op == "==" || op == "!="))
            {
                AddError(context.expression(0).Start, $"Cannot compare values of types '{left?.GetType()}' and '{right?.GetType()}'");
            }
            else
            {
                return op switch
                {
                    "==" => Equal(left, right),
                    "!=" => Different(left, right),
                    ">" => GreaterThan(left, right),
                    "<" => LessThan(left, right),
                    ">=" => GreaterThanOrEqual(left, right),
                    "<=" => LessThanOrEqual(left, right),
                    _ => throw new NotImplementedException()
                };
            }

            return base.VisitComparisonExpression(context);
        }

        public override object? VisitPlayerCallExpression(StoryParser.PlayerCallExpressionContext context)
        {
            var key = context.IDENTIFIER().Symbol;
            if (!Player.ContainsKey(key.Text))
                AddError(key, $"Player attribute not declared: '{key.Text}'");
            else
            {
                return Player[context.IDENTIFIER().GetText()];
            }
            return base.VisitPlayerCallExpression(context);
        }

        public override object? VisitVariableDeclaration(StoryParser.VariableDeclarationContext context)
        {
            var variable = context.assignment().IDENTIFIER().Symbol;
            if (Variables.ContainsKey(variable.Text))
                AddError(variable, $"Variable already declared: '{variable.Text}'");
            else
            {
                Variables[variable.Text] = Visit(context.assignment().expression())!;
            }
            return base.VisitVariableDeclaration(context);
        }

        public override object? VisitAssignment(StoryParser.AssignmentContext context)
        {
            var variable = context.IDENTIFIER().Symbol;
            if (!Variables.ContainsKey(variable.Text))
                AddError(variable, $"Variable does not exist: '{variable.Text}'");
            else
            {
                Variables[variable.Text] = Visit(context.expression())!;
            }
            return base.VisitAssignment(context);
        }

        public override object? VisitVariableCallExpression(StoryParser.VariableCallExpressionContext context)
        {
            var variable = context.IDENTIFIER().Symbol;
            if (!Variables.ContainsKey(variable.Text))
                AddError(variable, $"Variable does not exist: '{variable.Text}'");
            else if (Variables[variable.Text] is null)
                AddError(variable, $"Variable not initialized: '{variable.Text}'");
            else
                return Variables[variable.Text];
            return base.VisitVariableCallExpression(context);
        }

        public override object? VisitNegativeExpression(StoryParser.NegativeExpressionContext context)
        {
            var value = Visit(context.expression());
            if (value is double d)
                return -d;
            else
            {
                var symbol = context.expression().Start;
                AddError(symbol, $"Expression of type {value?.GetType()} cannot be negated.");
            }
            return base.VisitNegativeExpression(context);
        }

        public override object? VisitNotExpression(StoryParser.NotExpressionContext context)
        {
            var value = Visit(context.expression());
            if (value is bool b)
                return !b;
            else
            {
                var symbol = context.expression().Start;
                AddError(symbol, $"Expression of type {value?.GetType()} cannot be negated.");
            }
            return base.VisitNotExpression(context);
        }

        public override object? VisitBooleanExpression(StoryParser.BooleanExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            var op = context.BOOLOP().GetText();

            var bothBool = left is bool && right is bool;
            if(bothBool)
            {
                return op switch
                {
                    "and" => (bool)left! && (bool)right!,
                    "or" => (bool)left! || (bool)right!,
                    _ => throw new NotImplementedException()
                };
            }
            else
            {
                var symbol = context.expression(0).Start;
                AddError(symbol, $"Cannot do boolean operation on values of types '{left?.GetType()}' and '{right?.GetType()}'");
            }
            return base.VisitBooleanExpression(context);
        }
        #region protected methods
        private static (int, int) GetPosition(IToken symbol)
        {
            return (symbol.Line, symbol.Column + 1);
        }

        private void AddError(IToken? symbol, string msg)
        {
            var (line, column) = GetPosition(symbol!);
            var error = $"Semantic error(line: {line}, column: {column}): {msg}";
            if (!SemanticErrors.Contains(error)) SemanticErrors.Add(error);
        }

        private void AddErrorsForGoTos()
        {
            foreach (var goTo in _goTos)
            {
                var symbol = goTo.IDENTIFIER().Symbol;
                if (!ScenesNames.Contains(symbol.Text))
                    AddError(symbol, $"Scene '{symbol.Text}' does not exist.");
            }
        }

        protected static object? Multiply(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l * r;

            throw new NotImplementedException();
        }
        protected static object? Divide(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l / r;

            throw new NotImplementedException();
        }

        protected static object? Add(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l + r;

            if (left is string || right is string)
                return $"{left}{right}";

            throw new NotImplementedException();
        }

        protected static object? Subtract(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l - r;

            throw new NotImplementedException();
        }

        protected static bool Equal(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l == r;

            if (left is string ls && right is string rs)
                return ls.Equals(rs);

            if (left is bool lb && right is bool rb)
                return lb == rb;

            throw new NotImplementedException();
        }

        protected static bool Different(object? left, object? right) => !Equal(left, right);

        protected static bool GreaterThan(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l > r;

            if (left is string ls && right is string rs)
                return string.Compare(ls, rs) > 0;

            throw new NotImplementedException();
        }

        protected static bool LessThan(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l < r;

            if (left is string ls && right is string rs)
                return string.Compare(ls, rs) < 0;

            throw new NotImplementedException();
        }

        protected static bool GreaterThanOrEqual(object? left, object? right) => !LessThan(left, right);

        protected static bool LessThanOrEqual(object? left, object? right) => !GreaterThan(left, right);
        #endregion
    }
}
