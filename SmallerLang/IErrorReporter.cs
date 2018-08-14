using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    public interface IErrorReporter
    {
        bool ErrorOccurred { get; }

        void WriteError(string pError);
        void WriteError(string pError, TextSpan pSpan);
        void WriteWarning(string pWarning);
        void WriteWarning(string pWarning, TextSpan pSpan);
    }
}
