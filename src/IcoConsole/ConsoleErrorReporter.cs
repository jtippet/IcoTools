using Ico.Host;
using Microsoft.DotNet.Cli.Utils;

namespace Ico.Console
{
    public class ConsoleErrorReporter : IErrorReporter
    {
        public void ErrorLine(string message)
        {
            Reporter.Error.WriteLine($"Error: {message}".Red());
        }

        public void ErrorLine(string message, string fileName)
        {
            Reporter.Error.WriteLine($"{fileName}: Error: {message}".Red());
        }

        public void ErrorLine(string message, string fileName, uint frameNumber)
        {
            Reporter.Error.WriteLine($"{fileName}({frameNumber + 1}): Error: {message}".Red());
        }

        public void WarnLine(string message)
        {
            Reporter.Output.WriteLine($"Warning: {message}".Yellow());
        }

        public void WarnLine(string message, string fileName)
        {
            Reporter.Output.WriteLine($"{fileName}: Warning: {message}".Yellow());
        }

        public void WarnLine(string message, string fileName, uint frameNumber)
        {
            Reporter.Output.WriteLine($"{fileName}({frameNumber + 1}): Warning: {message}".Yellow());
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
    }
}
