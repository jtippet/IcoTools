namespace Ico.Host
{
    public interface IErrorReporter
    {
        void ErrorLine(string message);

        void ErrorLine(string message, string fileName);

        void ErrorLine(string message, string fileName, uint frameNumber);

        void WarnLine(string message);

        void WarnLine(string message, string fileName);

        void WarnLine(string message, string fileName, uint frameNumber);

        void InfoLine(string message);

        void VerboseLine(string message);
    }
}
