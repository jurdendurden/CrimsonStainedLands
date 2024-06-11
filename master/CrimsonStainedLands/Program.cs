﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Game.Launch(Settings.Port);
            while (!Game.Instance.Info.Exiting)
            {
                var log = Game.Instance.Info.RetrieveLog();
                if (!string.IsNullOrEmpty(log))
                {
                    Console.WriteLine(log.Trim());
                }
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}
