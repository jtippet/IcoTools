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
            Reporter.Error.WriteLine($"{fileName}({frameNumber}): Error: {message}".Red());
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
            Reporter.Output.WriteLine($"{fileName}({frameNumber}): Warning: {message}".Yellow());
        }

        public void InfoLine(string s)
        {
            Reporter.Output.WriteLine(s);
        }

        public void VerboseLine(string s)
        {
            Reporter.Verbose.WriteLine(s);
        }
    }
}
