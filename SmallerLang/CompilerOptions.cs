using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    public struct CompilerOptions
    {
        public string Source { get; set; }
        public string SourceFile { get; set; }
        public string OutputFile { get; set; }
        public bool Optimizations { get; set; }
        public bool OutputBytecode { get; set; }
        public bool Debug { get; set; }

        public CompilerOptions(string pOutputFile)
        {
            Source = "";
            SourceFile = "";
            OutputFile = pOutputFile;
            Optimizations = true;
            OutputBytecode = false;
            Debug = false;
        }

        public CompilerOptions(string pSource, string pOutputFile)
        {
            Source = pSource;
            SourceFile = "";
            OutputFile = pOutputFile;
            Optimizations = true;
            OutputBytecode = false;
            Debug = false;
        }
    }
}
