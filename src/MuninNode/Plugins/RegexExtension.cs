using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MuninNode.Plugins;

public static class RegexExtension
{
    public static void ThrowIfNotMatch(this Regex regex, string argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (regex.IsMatch(argument))
        {
            return;
        }
        throw new ArgumentException(
            $"'{argument}' is invalid for field name. The value of {nameof(argument)} must match the following regular expression: '{regex}'",
            nameof(argument));

    }
}