using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace SimpleStoryMaker
{
    public class SyntaxErrorListener : BaseErrorListener, IAntlrErrorListener<object>
    {
        public static bool HasError { get; private set; }
        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] object offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            HasError = true;
            var error = $"Syntax Error: (line: {line}, column: {charPositionInLine+1}) {msg}";
            Console.WriteLine(error);
        }
    }
}
