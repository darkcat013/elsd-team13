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
    var visitor = new StoryVisitor();
    visitor.Visit(program);

}