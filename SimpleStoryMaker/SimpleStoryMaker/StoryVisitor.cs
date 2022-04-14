using Antlr4.Runtime.Misc;
using SimpleStoryMaker.Content;

namespace SimpleStoryMaker
{
    public class StoryVisitor : InitialVisitor
    {
        public StoryVisitor(InitialVisitor initialVisitor)
        {
            Player = initialVisitor.Player;
            StartScene = initialVisitor.StartScene;
            Scenes = initialVisitor.Scenes;
            EndScene = initialVisitor.EndScene;
            ScenesNames = initialVisitor.ScenesNames;
            Variables = initialVisitor.Variables;
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
            return null;
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
                return VisitScene(Scenes.First(x => x.IDENTIFIER().GetText() == id.GetText()));
            else if (context.START() is { })
                return VisitStart(StartScene!);
            else
                return VisitEnd(EndScene!);
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

        public override object? VisitPlayerCallExpression(StoryParser.PlayerCallExpressionContext context)
        {
            return Player[context.IDENTIFIER().GetText()];
        }

        public override object? VisitAssignment(StoryParser.AssignmentContext context)
        {
            return null;
        }

        public override object? VisitVariableCallExpression(StoryParser.VariableCallExpressionContext context)
        {
            return Variables[context.IDENTIFIER().GetText()];
        }

        public override object? VisitNegativeExpression(StoryParser.NegativeExpressionContext context)
        {
            var value = Visit(context.expression());
            if (value is double d)
                return -d;

            throw new NotImplementedException();
        }

        public override object? VisitNotExpression(StoryParser.NotExpressionContext context)
        {
            var value = Visit(context.expression());
            if (value is bool b)
                return !b;
            throw new NotImplementedException();
        }

        public override object? VisitBooleanExpression(StoryParser.BooleanExpressionContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            var op = context.BOOLOP().GetText();

            return op switch
            {
                "and" => (bool)left! && (bool)right!,
                "or" => (bool)left! || (bool)right!,
                _ => throw new NotImplementedException()
            };
        }
    }
}
