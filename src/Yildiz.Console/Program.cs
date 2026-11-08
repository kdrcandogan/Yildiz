using System.Text;
using Yildiz.Console;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

int? seed = ParseOptionalIntArgument(args, "--seed");
int? delayMs = ParseOptionalIntArgument(args, "--delay-ms");
TimeSpan? stepDelay = delayMs.HasValue
    ? TimeSpan.FromMilliseconds(Math.Max(0, delayMs.Value))
    : null;

var application = new ConsoleApplication(fixedSeed: seed, stepDelay: stepDelay);
application.Run();

static int? ParseOptionalIntArgument(string[] arguments, string optionName)
{
    for (int i = 0; i < arguments.Length - 1; i++)
    {
        if (string.Equals(arguments[i], optionName, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(arguments[i + 1], out int parsedValue))
        {
            return parsedValue;
        }
    }

    return null;
}
