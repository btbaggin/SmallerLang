using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    /// <summary>
    /// Interface for the compiler to report errors and warnings during the compilation process.
    /// Errors should mark <see cref="ErrorOccurred"/> and will stop the compilation
    /// Warnings will not stop the compilation
    /// </summary>
    public interface IErrorReporter
    {
        /// <summary>
        /// Returns if an error has been reported
        /// </summary>
        bool ErrorOccurred { get; }

        /// <summary>
        /// Used to report an error
        /// </summary>
        /// <param name="pError">Error text to report</param>
        void WriteError(string pError);

        /// <summary>
        /// Used to report an error with location info
        /// </summary>
        /// <param name="pError">Error text to report</param>
        /// <param name="pSpan">Location at which the error occurred</param>
        void WriteError(string pError, TextSpan pSpan);

        /// <summary>
        /// Used to report a warning
        /// </summary>
        /// <param name="pWarning">Warning text to report</param>
        void WriteWarning(string pWarning);

        /// <summary>
        /// Used to report a warning with location info
        /// </summary>
        /// <param name="pWarning">Warning text to report</param>
        /// <param name="pSpan">Location at which the error occurred</param>
        void WriteWarning(string pWarning, TextSpan pSpan);
    }
}
