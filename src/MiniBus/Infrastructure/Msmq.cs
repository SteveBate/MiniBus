using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

using MiniBus.Exceptions;

namespace MiniBus.Infrastructure
{
    public static class Msmq
    {
        public static void Install()
        {
            string filename = GetOsDependantFilename();

            var startInfo = new ProcessStartInfo(filename, InstallArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetTempPath()
            };

            using (var process = new Process())
            {
                var output = new StringBuilder();

                process.StartInfo = startInfo;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)                        
                        output.AppendLine(e.Data);                        
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                ParseForElevatedPermissionsError(output.ToString());
                Console.Out.WriteLine(output.ToString());
            }            
        }

        public static bool IsInstalled 
        {
            get
            {
                return ServiceController.GetServices().Any(s => s.ServiceName == "MSMQ");
            }
        }

        static string GetOsDependantFilename()
        {
            // Windows 7 / 8 / 2008R2 / 2012
            var system32Directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "system32");

            // For 32-bit processes on 64-bit systems, %windir%\system32 folder can only be accessed by specifying %windir%\sysnative folder.
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                system32Directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "sysnative");

            // Windows 7 / 8 / 2008R2 / 2012
            string filename = system32Directory + "\\dism.exe";
            return filename;
        }

        static string ParseForElevatedPermissionsError(string output)
        {
            if (output.Contains("Error: 740"))
                throw new BusException(output);

            return output;
        }

        const string InstallArgs = @"/Online /NoRestart /English /Enable-Feature /all /FeatureName:MSMQ-Server";
    }
}
