using Antlr4.Runtime.Misc;
using SimpleStoryMaker.Content;

namespace SimpleStoryMaker
{
    public class StoryVisitor : StoryBaseVisitor<object?>
    {

        public override object? VisitText(StoryParser.TextContext context)
        {
            Console.WriteLine(context.STRING().GetText()[1..^1]);
            var input = Console.ReadLine();
            if (input == context.input().CHAR().GetText())
            {
                Console.WriteLine("good");
            }
            else Console.WriteLine("bad");
            return null;
        }
    }
}
