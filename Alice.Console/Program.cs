// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Genova.Alice.Console;

/// <summary>
/// Use the Windows Console as the user interface for the ALICE chatbot.
/// </summary>
internal class Program
{
    /// <summary>
    /// The main entry point for the Genova Alice.Console application.
    /// Runs an instance of the ALICE chatbot in the console.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Convention of the Main method.")]
    public static void Main(string[] args)
    {
        Alice alice = new();
        while (true)
        {
            System.Console.Write("You> ");
            string? line = System.Console.ReadLine();
            if (line == null) break;
            if (line.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

            string reply = alice.GetResponse(line);
            System.Console.WriteLine($"Alice> {reply}");
        }
    }
}
