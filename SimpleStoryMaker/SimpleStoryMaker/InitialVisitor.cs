using Antlr4.Runtime;
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
        public List<string> SemanticErrors { get; set; } = new();
        private List<StoryParser.GoToContext> _goTos = new();
        public override object? VisitAttribute(StoryParser.AttributeContext context)
        {
            var key = context.IDENTIFIER().GetText();

            if (Player.ContainsKey(key))
                AddError(context.IDENTIFIER().Symbol, $"Duplicate player attribute: '{key}'");

            Player[key] = default;
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
                AddError(context.IDENTIFIER().Symbol,$"Duplicate scene name: '{sceneName}'");

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

                AddError(symbol,$"Duplicate choice option in scene: '{symbol?.Text}'");
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

            return base.VisitAdditiveExpression(context);
        }

        public override object? VisitComparisonExpression(StoryParser.ComparisonExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            var op = context.COMPAREOP().GetText();

            var sameType = left?.GetType() == right?.GetType();
            if(!sameType)
            {
                AddError(context.expression(0).Start, $"Cannot compare values of types '{left?.GetType()}' and '{right?.GetType()}'");
            }
            else if(left is bool && !(op == "==" || op == "!="))
            {
                AddError(context.expression(0).Start, $"Cannot compare values of types '{left?.GetType()}' and '{right?.GetType()}'");
            }

            return base.VisitComparisonExpression(context);
        }

        #region private methods
        private static (int, int) GetPosition(IToken symbol)
        {
            return (symbol.Line, symbol.Column + 1);
        }

        private void AddError(IToken? symbol, string msg)
        {
            var (line, column) = GetPosition(symbol!);
            SemanticErrors.Add($"Semantic error(line: {line}, column: {column}): {msg}");
        }

        private void AddErrorsForGoTos()
        {
            foreach(var goTo in _goTos)
            {
                var symbol = goTo.IDENTIFIER().Symbol;
                if(!ScenesNames.Contains(symbol.Text))
                    AddError(symbol,$"Scene '{symbol.Text}' does not exist.");
            }
        }
        #endregion
    }
}
