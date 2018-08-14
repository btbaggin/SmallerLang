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
            if (pSpan.HasValue)
            {
                TextSpan s = pSpan.Value;
                Console.ForegroundColor = pErrorType ? ConsoleColor.Red : ConsoleColor.Yellow;

                if(pErrorType) Console.Write("Error occurred ");
                else Console.Write("Warning occurred ");
                Console.WriteLine($"at line: {s.Line.ToString()} column: {s.Column.ToString()}");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(pError);
                WriteSpanLine(s);
            }
            else
            {
                Console.ForegroundColor = pErrorType ? ConsoleColor.Red : ConsoleColor.Yellow;
                if (pErrorType) Console.WriteLine("Error occurred ");
                else Console.WriteLine("Warning occurred ");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(pError);
            }
            Console.WriteLine();
        }

        private void WriteSpanLine(TextSpan pSpan)
        {
            string line = pSpan.GetContainingLine(_source);
            Console.Write(line.Substring(0, pSpan.Column + 1).TrimStart());

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(line.Substring(pSpan.Column + 1, pSpan.Length));
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine(line.Substring(pSpan.Column + pSpan.Length + 1).TrimEnd());
        }
    }
}
