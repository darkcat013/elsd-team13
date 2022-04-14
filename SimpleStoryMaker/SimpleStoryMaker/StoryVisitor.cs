using Antlr4.Runtime.Misc;
using SimpleStoryMaker.Content;

namespace SimpleStoryMaker
{
    public class StoryVisitor : StoryBaseVisitor<object?>
    {
        private readonly Dictionary<string, object?> _player;
        private readonly StoryParser.StartContext? _startScene;
        private readonly List<StoryParser.SceneContext> _scenes;
        private readonly StoryParser.EndContext? _endScene;
        private readonly List<string> _scenesNames;
        public StoryVisitor(InitialVisitor semanticCheck)
        {
            _player = semanticCheck.Player;
            _startScene = semanticCheck.StartScene;
            _scenes = semanticCheck.Scenes;
            _endScene = semanticCheck.EndScene;
            _scenesNames = semanticCheck.ScenesNames;
        }
        public override object? VisitStart(StoryParser.StartContext context)
        {
            Console.WriteLine(Visit(context.write().expression()));
            return base.VisitStart(context);
        }
        public override object? VisitEnd(StoryParser.EndContext context)
        {
            Console.WriteLine(Visit(context.write().expression()));
            Environment.Exit(0);
            return null;
        }

        public override object? VisitAttribute(StoryParser.AttributeContext context)
        {
            var key = context.IDENTIFIER().GetText();

            var value = Visit(context.expression());

            _player[key] = value;

            return base.VisitAttribute(context);
        }

        public override object? VisitScene(StoryParser.SceneContext context)
        {
            Console.WriteLine(Visit(context.write().expression()));
            return base.VisitScene(context);
        }

        public override object? VisitChoices(StoryParser.ChoicesContext context)
        {
            var choices = context.choice();

            var choicesString = choices.Select(x => x.IDENTIFIER().GetText()).ToList() as List<string?>;

            foreach (var item in choices)
            {
                Console.WriteLine($"{item.IDENTIFIER()}: {Visit(item.choiceText().expression())}");
            }
            var input = "";
            var validInput = false;
            do
            {
                Console.Write(">:");
                input = Console.ReadLine();
                validInput = choicesString.Contains(input);
                if (!validInput)
                {
                    Console.WriteLine("Choice unavailable");
                }
                Console.WriteLine();
            } while (!validInput);

            var choice = choices.First(x => x.IDENTIFIER().GetText() == input);
            var go = choice.goTo();

            return VisitGoTo(go);
        }

        public override object? VisitGoTo(StoryParser.GoToContext context)
        {
            if (context.IDENTIFIER() is { } id)
                return VisitScene(_scenes.First(x => x.IDENTIFIER().GetText() == id.GetText()));
            else if (context.START() is { })
                return VisitStart(_startScene!);
            else
                return VisitEnd(_endScene!);
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

        public override object? VisitParanthesizedExpression(StoryParser.ParanthesizedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override object? VisitMultiplicativeExpression(StoryParser.MultiplicativeExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            var op = context.MULTOP().GetText();

            return op switch
            {
                "*" => Multiply(left, right),
                "/" => Divide(left, right),
                _ => throw new NotImplementedException()
            };
        }

        public override object? VisitAdditiveExpression(StoryParser.AdditiveExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            var op = context.ADDOP().GetText();

            return op switch
            {
                "+" => Add(left, right),
                "-" => Subtract(left, right),
                _ => throw new NotImplementedException()
            };
        }

        public override object? VisitComparisonExpression(StoryParser.ComparisonExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            var op = context.COMPAREOP().GetText();

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

        #region private methods
        private static object? Multiply(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l * r;

            throw new NotImplementedException();
        }
        private static object? Divide(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l / r;

            throw new NotImplementedException();
        }

        private static object? Add(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l + r;

            if (left is string || right is string)
                return $"{left}{right}";

            throw new NotImplementedException();
        }

        private static object? Subtract(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l - r;

            throw new NotImplementedException();
        }

        private static bool Equal(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l == r;

            if (left is string ls && right is string rs)
                return ls.Equals(rs);

            if (left is bool lb && right is bool rb)
                return lb == rb;

            throw new NotImplementedException();
        }

        private static bool Different(object? left, object? right) => !Equal(left, right);

        private static bool GreaterThan(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l > r;

            if (left is string ls && right is string rs)
                return string.Compare(ls, rs) > 0;

            throw new NotImplementedException();
        }

        private static bool LessThan(object? left, object? right)
        {
            if (left is double l && right is double r)
                return l < r;

            if (left is string ls && right is string rs)
                return string.Compare(ls, rs) < 0;

            throw new NotImplementedException();
        }

        private static bool GreaterThanOrEqual(object? left, object? right) => !LessThan(left, right);

        private static bool LessThanOrEqual(object? left, object? right) => !GreaterThan(left, right);
        #endregion
    }
}
