using Antlr4.Runtime;
using SimpleStoryMaker;
using SimpleStoryMaker.Content;

var fileName = "Content\\test.ssm";

var fileContents = File.ReadAllText(fileName);

var inputStream = new AntlrInputStream(fileContents);
var storyLexer = new StoryLexer(inputStream);
var commonTokenStream = new CommonTokenStream(storyLexer);

var storyParser = new StoryParser(commonTokenStream);
storyParser.RemoveErrorListeners();
storyParser.AddErrorListener(new SyntaxErrorListener());

var program = storyParser.program();

if (SyntaxErrorListener.HasError)
{
    Console.WriteLine("Program exited");
}
else
{
    var semanticCheck = new InitialVisitor();
    semanticCheck.Visit(program);

    if (semanticCheck.SemanticErrors.Count > 0)
    {
        foreach (var message in semanticCheck.SemanticErrors)
        {
            Console.WriteLine(message);
        }
    }
    else
    {
        var visitor = new StoryVisitor(semanticCheck);
        visitor.Visit(program);
        Console.WriteLine("done");
    }
}