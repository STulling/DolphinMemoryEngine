using DolphinMemoryEngine;
using DolphinMemoryEngine.DolphinProcess;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
WindowsDolphinProcess process = new WindowsDolphinProcess();
Console.WriteLine(process.findPID());
