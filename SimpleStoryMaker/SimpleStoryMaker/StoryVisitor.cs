using Antlr4.Runtime.Misc;
using SimpleStoryMaker.Content;

namespace SimpleStoryMaker
{
    public class StoryVisitor : StoryBaseVisitor<object?>
    {
        public Dictionary<string, object?> Player { get; set; } = new();
        public StoryParser.StartContext? StartScene { get; set; }
        public List<StoryParser.SceneContext> Scenes { get; set; } = new();
        public StoryParser.EndContext? EndScene { get; set; }
        public List<string> ScenesNames { get; set; } = new();
        public List<string> SemanticErrors { get; private set; } = new();
        public Dictionary<string, object> Variables { get; set; } = new();

        protected int LocalScope = 0;
        protected Dictionary<string, object> LocalVariables { get; set; } = new();

        private readonly Random rand = new();
        public StoryVisitor(InitialVisitor initialVisitor)
        {
            Player = initialVisitor.Player;
            StartScene = initialVisitor.StartScene;
            Scenes = initialVisitor.Scenes;
            EndScene = initialVisitor.EndScene;
            ScenesNames = initialVisitor.ScenesNames;
            Variables = initialVisitor.Variables;
            LocalVariables = new();
            LocalScope = 0;
        }
        public override object? VisitEnd(StoryParser.EndContext context)
        {
            base.VisitEnd(context);
            Environment.Exit(0);
            return null;
        }

        public override object? VisitAttribute(StoryParser.AttributeContext context)
        {
            return null;
        }

        public override object? VisitWrite([NotNull] StoryParser.WriteContext context)
        {
            Console.WriteLine(Visit(context.expression()));
            Console.WriteLine();
            return null;
        }
        public override object? VisitChoices(StoryParser.ChoicesContext context)
        {
            var choices = context.choice();

            var choicesString = choices.Select(x => x.IDENTIFIER().GetText()).ToList() as List<string?>;

            foreach (var item in choices)
            {
                var hasCondition = item.choiceCondition() != null;
                var show = true;
                if (hasCondition) show = (bool)Visit(item.choiceCondition().expression())!;

                if (show)
                Console.WriteLine($"{item.IDENTIFIER()}: {Visit(item.choiceScope().choiceText().expression())}");
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
                else
                {
                    var choice = choices.First(x => x.IDENTIFIER().GetText() == input);
                    if (choice.choiceCondition() is { })
                    {
                        var choiceAvailable = Visit(choice.choiceCondition().expression());
                        if (choiceAvailable is bool b && !b)
                        {
                            Console.WriteLine("Choice unavailable");
                            validInput = false;
                        }
                    }
                }
                Console.WriteLine();
            } while (!validInput);

            var finalChoice = choices.First(x => x.IDENTIFIER().GetText() == input);
            Visit(finalChoice.choiceScope());
            if (finalChoice.choiceScope().write() is { }) Visit(context);
            return null;
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

        public override object? VisitPlayerAttributeAssignment(StoryParser.PlayerAttributeAssignmentContext context)
        {
            var attribute = context.IDENTIFIER().Symbol.Text;
            Player[attribute] = Visit(context.expression());
            return null;
        }
        public override object? VisitAssignment(StoryParser.AssignmentContext context)
        {
            var variable = context.IDENTIFIER().Symbol;
            if (LocalScope > 0)
            {
                if (Variables.ContainsKey(variable.Text)) Variables[variable.Text] = Visit(context.expression())!;
                else LocalVariables[variable.Text] = Visit(context.expression())!;
            }
            else
                Variables[variable.Text] = Visit(context.expression())!;

            return null;
        }

        public override object? VisitVariableCallExpression(StoryParser.VariableCallExpressionContext context)
        {
            var variable = context.IDENTIFIER().Symbol;
            if (LocalScope > 0 && LocalVariables.ContainsKey(variable.Text))
                return LocalVariables[variable.Text];
            else
                return Variables[variable.Text];
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

        public override object? VisitIfBlock(StoryParser.IfBlockContext context)
        {
            if (Visit(context.@if().expression()) is bool b && b)
            {
                Visit(context.@if().localScope());
            }
            else
            {
                var elifs = context.elif();
                var visited = false;
                if (elifs is { } && elifs.Any())
                {
                    foreach (var elif in elifs)
                    {
                        if (!visited && Visit(elif.expression()) is bool b1 && b1)
                        {
                            Visit(elif.localScope());
                            visited = true;
                        }
                    }
                }
                if (!visited)
                {
                    if (context.@else() is { } e)
                    {

                        Visit(e.localScope());
                    }
                }
            }

            return null;
        }
        public override object? VisitWhileBlock(StoryParser.WhileBlockContext context)
        {
            var b = (bool)Visit(context.expression())!;
            while (b)
            {
                Visit(context.localScope());
                b = (bool)Visit(context.expression())!;
            }
            return null;
        }
        public override object? VisitLocalDeclaration(StoryParser.LocalDeclarationContext context)
        {
            var variable = context.assignment().IDENTIFIER().Symbol.Text;
            LocalVariables[variable] = Visit(context.assignment().expression())!;
            return null;
        }
        public override object? VisitSceneScope(StoryParser.SceneScopeContext context)
        {
            LocalScope++;
            base.VisitSceneScope(context);
            LocalScope--;
            if (LocalScope == 0) LocalVariables = new();
            return null;
        }

        public override object? VisitEndSceneScope(StoryParser.EndSceneScopeContext context)
        {
            LocalScope++;
            base.VisitEndSceneScope(context);
            LocalScope--;
            if (LocalScope == 0) LocalVariables = new();
            return null;
        }

        public override object? VisitChoiceScope(StoryParser.ChoiceScopeContext context)
        {
            LocalScope++;
            base.VisitChoiceScope(context);
            LocalScope--;
            if (LocalScope == 0) LocalVariables = new();
            return null;
        }

        public override object? VisitRand_func(StoryParser.Rand_funcContext context)
        {
            var min = (double)Visit(context.expression(0))!;
            var max = (double)Visit(context.expression(1))!;
            return rand.NextDouble() * (max - min) + min;
        }

        #region protected methods
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
