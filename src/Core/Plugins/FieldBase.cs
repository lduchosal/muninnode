// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.RegularExpressions;

namespace MuninNode.Plugins;

public class FieldBase
{
    public required string Name { get; 
        init
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(Name));
            RegexValidFieldName.ThrowIfNotMatch(value, nameof(Name));
            field = value;
        } 
    }

    /// <summary>Gets a value for the <c>{fieldname}.label</c>.</summary>
    /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-label">Plugin reference - Field name attributes - {fieldname}.label</seealso>
    public required string Label { get;
        init
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(Label));
            RegexValidFieldLabel.ThrowIfNotMatch(value, nameof(Label));
            field = value;
        }
    }

    public Func<double?> FetchValue { get; init; } = () => default;

    /// <summary>Gets a value for the <c>{fieldname}.draw</c>.</summary>
    /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-draw">Plugin reference - Field name attributes - {fieldname}.draw</seealso>
    /// <seealso cref="Plugins.GraphStyle"/>
    public required GraphStyle GraphStyle { get; init; }

    /// <summary>Gets a value for the <c>{fieldname}.warning</c>.</summary>
    /// <remarks>This property defines the upper limit, lower limit, or range of normal value, that is not treated as warning.</remarks>
    /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-warning">Plugin reference - Field name attributes - {fieldname}.warning</seealso>
    /// <seealso cref="ValueRange"/>
    public required ValueRange WarningRange { get; init; }

    /// <summary>Gets a value for the <c>{fieldname}.critical</c>.</summary>
    /// <remarks>This property defines the upper limit, lower limit, or range of normal value, that is not treated as critical.</remarks>
    /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-critical">Plugin reference - Field name attributes - {fieldname}.critical</seealso>
    /// <seealso cref="ValueRange"/>
    public required ValueRange CriticalRange { get; init; }

    /// <summary>Gets a value for the <c>{fieldname}.negative</c>.</summary>
    /// <remarks>
    /// This property specifies that the specified field is drawn as the negative side of this field.
    /// If a valid field name is specified for this property, it also implicitly sets the attribute <c>{fieldname}.graph no</c>.
    /// </remarks>
    /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-negative">Plugin reference - Field name attributes - {fieldname}.critical</seealso>
    /// <seealso href="https://guide.munin-monitoring.org/en/latest/develop/plugins/plugin-bcp.html#plugin-bcp-direction">Best Current Practices for good plugin graphs - Direction</seealso>
    public string? NegativeFieldName { get; init; }

    public async Task<string> GetFormattedValueStringAsync(CancellationToken cancellationToken)
    {
        const string unknownValueString = "U";
        var value = await FetchValueAsync(cancellationToken).ConfigureAwait(false);
        return value?.ToString(provider: CultureInfo.InvariantCulture) ?? unknownValueString;
    }

    // https://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes
    // Field name attributes
    //   Attribute: {fieldname}.label
    //   Value: anything except # and \
    private static readonly Regex RegexValidFieldLabel = new(
        pattern: $@"^[^{Regex.Escape("#\\")}]+$",
        options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

// https://guide.munin-monitoring.org/en/latest/reference/plugin.html#notes-on-field-names
// Notes on field names
//   The characters must be [a-zA-Z0-9_], while the first character must be [a-zA-Z_].
    private static readonly Regex RegexValidFieldName = new(
        pattern: @"^[a-zA-Z_][a-zA-Z0-9_]*$",
        options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex RegexInvalidFieldNamePrefix = new(
        pattern: @"^[0-9_]+",
        options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex RegexInvalidFieldNameChars = new(
        pattern: @"[^a-zA-Z0-9_]",
        options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static string GetDefaultNameFromLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            throw new ArgumentNullException(nameof(label));
        }

        return RegexInvalidFieldNameChars.Replace(
            RegexInvalidFieldNamePrefix.Replace(label, string.Empty),
            string.Empty
        );
    }

    protected ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken)
        => new(FetchValue());

}