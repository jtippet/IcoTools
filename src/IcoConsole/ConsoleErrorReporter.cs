using System;
using System.Collections.Generic;
using Ico.Host;
using Ico.Validation;
using Microsoft.DotNet.Cli.Utils;

namespace Ico.Console
{
    public class ConsoleErrorReporter : IErrorReporter
    {
        public ISet<IcoErrorCode> WarningsToIgnore { get; } = new HashSet<IcoErrorCode>();

        public void ErrorLine(IcoErrorCode code, string message)
        {
            if (WarningsToIgnore.Contains(code))
                return;
            Reporter.Error.WriteLine($"Error{GenerateCode(code)}: {message}".Red());
        }

        public void ErrorLine(IcoErrorCode code, string message, string fileName)
        {
            if (WarningsToIgnore.Contains(code))
                return;
            Reporter.Error.WriteLine($"{fileName}: Error{GenerateCode(code)}: {message}".Red());
        }

        public void ErrorLine(IcoErrorCode code, string message, string fileName, uint frameNumber)
        {
            if (WarningsToIgnore.Contains(code))
                return;
            Reporter.Error.WriteLine($"{fileName}({frameNumber + 1}): Error{GenerateCode(code)}: {message}".Red());
        }

        public void WarnLine(IcoErrorCode code, string message)
        {
            if (WarningsToIgnore.Contains(code))
                return;
            Reporter.Output.WriteLine($"Warning{GenerateCode(code)}: {message}".Yellow());
        }

        public void WarnLine(IcoErrorCode code, string message, string fileName)
        {
            if (WarningsToIgnore.Contains(code))
                return;
            Reporter.Output.WriteLine($"{fileName}: Warning{GenerateCode(code)}: {message}".Yellow());
        }

        public void WarnLine(IcoErrorCode code, string message, string fileName, uint frameNumber)
        {
            if (WarningsToIgnore.Contains(code))
                return;
            Reporter.Output.WriteLine($"{fileName}({frameNumber + 1}): Warning{GenerateCode(code)}: {message}".Yellow());
        }

        public void InfoLine(string message)
        {
            Reporter.Output.WriteLine(message);
        }

        public void InfoLine(string message, string fileName)
        {
            Reporter.Output.WriteLine($"{fileName}: {message}");
        }

        public void InfoLine(string message, string fileName, uint frameNumber)
        {
            Reporter.Output.WriteLine($"{fileName}({frameNumber + 1}): {message}");
        }

        public void VerboseLine(string message)
        {
            Reporter.Verbose.WriteLine(message);
        }

        public void VerboseLine(string message, string fileName)
        {
            Reporter.Verbose.WriteLine($"{fileName}: {message}");
        }

        public void VerboseLine(string message, string fileName, uint frameNumber)
        {
            Reporter.Verbose.WriteLine($"{fileName}({frameNumber + 1}): {message}");
        }

        private readonly SortedSet<IcoErrorCode> codesUsed = new SortedSet<IcoErrorCode>();

        public void PrintHelpUrls()
        {
            if (codesUsed.Count == 0)
                return;

            Reporter.Output.WriteLine("More information online:");

            foreach (var code in codesUsed)
            {
                Reporter.Output.WriteLine($"    ICO{(uint)code}: https://github.com/jtippet/IcoTools/wiki/ICO{(uint)code}");
            }
        }

        private string GenerateCode(IcoErrorCode code)
        {
            if (code == IcoErrorCode.NoError)
            {
                return "";
            }
            else
            {
                codesUsed.Add(code);
                return $" ICO{(uint)code}";
            }
        }
    }
}
