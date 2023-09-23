// Copyright (C) 2023 Jefferson Montgomery
// SPDX-License-Identifier: MIT
using System;

namespace reaper
{
    public static class ColorConsole
    {
        public static ConsoleColor Gray    = ConsoleColor.DarkGray;
        public static ConsoleColor Blue    = ConsoleColor.Blue;
        public static ConsoleColor Green   = ConsoleColor.Green;
        public static ConsoleColor Red     = ConsoleColor.Red;
        public static ConsoleColor Yellow  = ConsoleColor.DarkYellow;

        public static void Initialize()
        {
            // Adjust colors if background conflicts
            switch (Console.BackgroundColor) {
            case ConsoleColor.DarkBlue:
            case ConsoleColor.DarkCyan:
            case ConsoleColor.Blue:
            case ConsoleColor.Cyan:
                Blue = ConsoleColor.Black;
                break;

            case ConsoleColor.DarkGreen:
            case ConsoleColor.Green:
                Green = ConsoleColor.Black;
                break;

            case ConsoleColor.DarkRed:
            case ConsoleColor.Red:
                Red = ConsoleColor.Black;
                break;

            case ConsoleColor.DarkYellow:
            case ConsoleColor.Yellow:
                Yellow = ConsoleColor.Black;
                break;

            case ConsoleColor.DarkGray:
                Gray = ConsoleColor.Gray;
                Blue = ConsoleColor.DarkBlue;
                break;
            }
        }

        public static void Write(ConsoleColor color, string msg) { Console.ForegroundColor = color; Console.Write(msg); Console.ResetColor(); }
        public static void WriteError(ConsoleColor color, string msg) { Console.ForegroundColor = color; Console.Error.Write(msg); Console.ResetColor(); }
        public static void WriteLine(ConsoleColor color, string msg) { Console.ForegroundColor = color; Console.WriteLine(msg); Console.ResetColor(); }
        public static void WriteLineError(ConsoleColor color, string msg) { Console.ForegroundColor = color; Console.Error.WriteLine(msg); Console.ResetColor(); }

        public static void WriteColorTest()
        {
            for (int i = 0; i < 16; ++i) {
                Console.Write($"        {i.ToString().PadLeft(2)}");
            }
            Console.WriteLine();

            for (int i = 0; i < 16; ++i) {
                for (int j = 0; j < 16; ++j) {
                    Console.Write($" {i.ToString().PadLeft(2)}");
                    Console.ForegroundColor = (ConsoleColor) i;
                    Console.BackgroundColor = (ConsoleColor) j;
                    Console.Write(" Hello ");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
    }
}