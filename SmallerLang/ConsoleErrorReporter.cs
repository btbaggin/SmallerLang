using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    class ConsoleErrorReporter : IErrorReporter
    {
        public bool ErrorOccurred { get; private set; }

        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;

        public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;

        public ConsoleColor TextColor { get; set; } = ConsoleColor.White;

        public ConsoleColor SpanColor { get; set; } = ConsoleColor.Cyan;

        readonly string _source;
        public ConsoleErrorReporter(string pSource)
        {
            _source = pSource;
            ErrorOccurred = false;
        }

        public void WriteError(string pError)
        {
            DisplayError(pError, null, true);
            ErrorOccurred = true;
        }

        public void WriteError(string pError, TextSpan pSpan)
        {
            DisplayError(pError, pSpan, true);
            ErrorOccurred = true;
        }

        public void WriteWarning(string pWarning)
        {
            DisplayError(pWarning, null, false);
        }

        public void WriteWarning(string pWarning, TextSpan pSpan)
        {
            DisplayError(pWarning, pSpan, false);
        }

        private void DisplayError(string pError, TextSpan? pSpan, bool pErrorType)
        {
            Console.ForegroundColor = pErrorType ? ErrorColor : WarningColor;
            if (pErrorType) Console.Write("Error occurred ");
            else Console.Write("Warning occurred ");

            if (pSpan.HasValue)
            {
                TextSpan s = pSpan.Value;
                Console.WriteLine($"at line: {s.Line.ToString()} column: {s.Column.ToString()}");

                Console.ForegroundColor = TextColor;
                Console.WriteLine(pError);
                WriteSpanLine(s);
            }
            else
            {
                Console.WriteLine();
                Console.ForegroundColor = TextColor;
                Console.WriteLine(pError);
            }
            Console.WriteLine();
        }

        private void WriteSpanLine(TextSpan pSpan)
        {
            string line = pSpan.GetContainingLine(_source);
            Console.Write(line.Substring(0, pSpan.Column + 1).TrimStart());

            Console.ForegroundColor = SpanColor;
            Console.Write(line.Substring(pSpan.Column + 1, pSpan.Length));
            Console.ForegroundColor = TextColor;

            Console.WriteLine(line.Substring(pSpan.Column + pSpan.Length + 1).TrimEnd());
        }
    }
}
