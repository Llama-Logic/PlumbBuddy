using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbBuddy.App.Platforms.MacCatalyst;

class PlatformFunctions :
    IPlatformFunctions
{
    public StringComparison FileSystemStringComparison =>
        StringComparison.Ordinal;

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("open", directoryInfo.FullName);

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("open", $"-R \"{fileInfo.FullName}\"");
}
