using Ganss.IO;
using Ico.Host;
using Ico.Validation;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;

namespace Ico.Console
{
    public static class FileGlobExpander
    {
        public static IEnumerable<FileSystemInfoBase> Expand(IEnumerable<string> globs, IErrorReporter reporter)
        {
            var files = new List<FileSystemInfoBase>();

            foreach (var glob in globs)
            {
                foreach (var path in Glob.Expand(glob))
                {
                    if (!files.Contains(path))
                    {
                        files.Add(path);
                    }
                }
            }

            if (files.Count == 0)
            {
                reporter.ErrorLine(IcoErrorCode.FileNotFound, "No files matched the inputs.");
                return null;
            }

            return files;
        }
    }
}
