using System;
using System.Collections.Generic;
using System.Text;

namespace XiaoStatusLed
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine(
                    "Usage: ClaudeCodeLed <state>");

                return;
            }

            string state =
                args[0].ToLowerInvariant();

            XiaoCom xiao = new XiaoCom();

            if (!xiao.Connect())
            {
                Console.Error.WriteLine(
                    "XIAO Status LED was not found.");

                return;
            }

            try
            {
                switch (state)
                {
                    case "working":
                        xiao.SetPattern(
                            0, 0, 255,
                            "SINE",
                            2000,
                            30,
                            255);
                        break;

                    case "waiting":
                        xiao.SetPattern(
                            255, 128, 0,
                            "SINE",
                            500,
                            0,
                            255);
                        break;

                    case "success":
                        xiao.SetPattern(
                            0, 255, 0,
                            "CONSTANT",
                            1000,
                            255,
                            255);
                        break;

                    case "error":
                        xiao.SetPattern(
                            255, 0, 0,
                            "SINE",
                            500,
                            0,
                            255);
                        break;

                    case "off":
                        xiao.SetPattern(
                            0, 0, 0,
                            "CONSTANT",
                            1000,
                            0,
                            0);
                        break;

                    default:
                        Console.Error.WriteLine(
                            $"Unknown state: {state}");

                        return;
                }
            }
            finally
            {
                xiao.Disconnect();
            }
        }
    }
}
