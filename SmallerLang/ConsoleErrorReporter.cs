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

        public ConsoleErrorReporter()
        {
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
                var path = System.IO.Path.GetFileName(s.Path);
                Console.WriteLine($"in file '{path}' at line: {s.Line.ToString()} column: {s.Column.ToString()}");

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

            var spanStart = pSpan.Column;
            if (pSpan.Start != pSpan.Column) spanStart++;

            string line = pSpan.GetContainingLine();
            Console.Write(line.Substring(0, spanStart).TrimStart());

            Console.ForegroundColor = SpanColor;
            Console.Write(line.Substring(spanStart, pSpan.Length));
            Console.ForegroundColor = TextColor;

            Console.WriteLine(line.Substring(spanStart + pSpan.Length).TrimEnd());
        }
    }
}
