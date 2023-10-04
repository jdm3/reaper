// Copyright (C) 2023 Jefferson Montgomery
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.Management;
using System.Threading;

using CConsole = reaper.ColorConsole;

namespace reaper
{
    internal class reaper
    {
        static void PrintUsage(string error, int code)
        {
            if (error != null) {
                Console.ForegroundColor = CConsole.Red;
                Console.Error.WriteLine(error);
                Console.ResetColor();
            }

            Console.WriteLine("usage: reaper.exe [options] NAME LIFESPAN");
            Console.WriteLine("options:");
            Console.WriteLine("    NAME        The name of the target process.");
            Console.WriteLine("    LIFESPAN    The maximum life span for target processes in seconds (default=0).");
            Console.WriteLine("    --wait      Process any existing processes, and then keep running to watch for new processes.");

            Environment.Exit(code);
        }

        static bool wait_ = false;
        static object obj_ = new object();

        static bool Kill(int pid)
        {
            if (pid > 0) {
                // Kill any children
                using(var searcher = new ManagementObjectSearcher($"SELECT ProcessId FROM Win32_Process WHERE ParentProcessID={pid}")) {
                    foreach (var item in searcher.Get()) {
                        Kill(Convert.ToInt32(item["ProcessID"]));
                    }
                }

                // Kill process if it's still running
                try {
                    var p = Process.GetProcessById(pid);
                    if (!p.HasExited) {
                        p.Kill();
                        p.WaitForExit();
                        return true;
                    }
                } catch (Exception) {}
            }

            return false;
        }

        static bool HasArgPrefix(string arg, out int startIdx)
        {
            startIdx = 1;

            if (arg.Length > 0) {
                switch (arg[0]) {
                case '-':
                    if (arg.Length > 1 && arg[1] == '-') {
                        startIdx = 2;
                    }
                    return true;
                case '/':
                    return true;
                }
            }

            return false;
        }

        static int Main(string[] args)
        {
            // Pick console colors
            CConsole.Initialize();

            // Parse command line arguments
            if (args.Length == 0) {
                PrintUsage(null, -1);
            }

            string processName = null;
            TimeSpan lifespan = new TimeSpan(0);
            bool lifespanSet = false;
            for (int i = 0; i < args.Length; ++i) {
                var arg = args[i];

                if (HasArgPrefix(arg, out var startIdx)) {
                    if (string.Compare(arg, startIdx, "wait", 0, 4, false) == 0) {
                        wait_ = true;
                        continue;
                    }

                    if (string.Compare(arg, startIdx, "help", 0, 4, false) == 0 ||
                        string.Compare(arg, startIdx, "h", 0, 1, false) == 0 ||
                        string.Compare(arg, startIdx, "?", 0, 1, false) == 0) {
                        PrintUsage(null, 0);
                    }
                } else {
                    if (processName == null) {
                        processName = arg;
                        continue;
                    }
                    if (!lifespanSet) {
                        try {
                            lifespan = TimeSpan.FromSeconds(int.Parse(arg));
                            lifespanSet = true;
                        }
                        catch (Exception) {
                            PrintUsage($"error: invalid time span: {arg}", -1);
                        }
                        continue;
                    }
                }

                PrintUsage($"error: invalid argument: {arg}", -1);
            }

            if (processName == null) {
                PrintUsage("error: NAME is required", -1);
            }

            int count = 0;

            // Capture ctrl-c and exit with current count
            if (wait_) {
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                    e.Cancel = true;
                    lock (obj_) {
                        wait_ = false;
                        Monitor.Pulse(obj_);
                    }
                };
            }

            // Query for all processes matching the specified name
            var dotPrinted = false;
            using (var searcher = new ManagementObjectSearcher($"SELECT ProcessId,CreationDate,CommandLine FROM Win32_Process WHERE Name='{processName}'")) {
                var roundTicks = TimeSpan.FromMilliseconds(10.0).Ticks;

                while (true) {
                    var now = DateTime.Now;
                    var waitTicks = lifespan.Ticks;
                    var first = true;
                    foreach (var item in searcher.Get()) {

                        // Get the current age of the process
                        var start = ManagementDateTimeConverter.ToDateTime(item["CreationDate"].ToString());
                        var age = now - start;
                        var leftTicks = lifespan.Ticks - age.Ticks;

                        // Kill the process if it is too old
                        if (leftTicks < 0) {
                            Kill(Convert.ToInt32(item["ProcessID"]));
                            count++;
                        } else {
                            leftTicks = ((leftTicks + roundTicks - 1) / roundTicks) * roundTicks;
                            waitTicks = Math.Min(waitTicks, leftTicks);
                        }

                        // Print the process info
                        if (first) {
                            first = false;
                            if (dotPrinted) {
                                dotPrinted = false;
                                Console.WriteLine();
                            }
                            CConsole.WriteLine(CConsole.Blue, $"{now}");
                        }

                        CConsole.WriteLine(leftTicks < 0 ? CConsole.Red : CConsole.Gray, $"{age} {item["CommandLine"]}");
                    }

                    // Exit if we're not waiting (or ctrl-c since last iteration)
                    if (!wait_) {
                        break;
                    }

                    // If no processes were found, print a dot to show we're still running
                    if (first) {
                        CConsole.Write(CConsole.Gray, ".");
                        dotPrinted = true;
                    }

                    // Sleep for one lifespan
                    lock (obj_) {
                        Monitor.Wait(obj_, new TimeSpan(waitTicks));
                    }
                }
            }

            // Print and return how many processes were killed.
            if (dotPrinted) {
                dotPrinted = false;
                Console.WriteLine();
            }

            CConsole.WriteLine(CConsole.Gray, $"{count} processes killed");
            return count;
        }
    }
}