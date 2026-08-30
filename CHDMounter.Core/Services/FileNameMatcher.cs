namespace CHDMounter.Core.Services;

/// <summary>
///     Matches file names against Windows directory-query search expressions.
/// </summary>
/// <remarks>
///     Windows does not pass plain "*.cue" patterns for directory enumeration.
///     For FileBothDirectoryInformation queries the I/O manager converts the
///     pattern into the NT "8.3 DOS wildcard" form where '*' becomes '&lt;'
///     (DOS_STAR), '?' becomes '&gt;' (DOS_QM) and the extension separator is
///     handled with '"' (DOS_DOT). A matcher that only understands '*' and '?'
///     therefore silently returns no matches for live queries such as
///     <c>dir Z:\*.cue</c>. This implementation is a port of Dokan's
///     <c>DokanIsNameInExpression</c> (dokan/directory.c) and supports the DOS
///     wildcard characters in addition to the regular '*' and '?'.
/// </remarks>
public static class FileNameMatcher
{
    private const char DosStar = '<';
    private const char DosQm = '>';
    private const char DosDot = '"';

    /// <summary>
    ///     Returns <c>true</c> if <paramref name="name" /> matches
    ///     <paramref name="expression" /> (case-insensitive), using the same rules
    ///     as the Windows file system pattern matcher.
    /// </summary>
    /// <param name="name">The file or directory name (without path).</param>
    /// <param name="expression">The search expression/pattern.</param>
    /// <returns><c>true</c> if the name matches the expression.</returns>
    public static bool IsMatch(string name, string expression)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(expression);

        return IsMatch(name.AsSpan(), expression.AsSpan());
    }

    private static bool IsMatch(ReadOnlySpan<char> name, ReadOnlySpan<char> expression)
    {
        while (expression.Length > 0)
        {
            var e = expression[0];

            switch (e)
            {
                case '*':
                {
                    expression = expression[1..];
                    if (expression.Length == 0)
                        return true;

                    for (var i = 0; i <= name.Length; i++)
                        if (IsMatch(name[i..], expression))
                            return true;

                    return false;
                }
                case DosStar:
                {
                    expression = expression[1..];

                    // DOS_STAR matches any characters up to (but not including)
                    // the last dot in the name; the rest of the expression then
                    // continues from the last dot (or from the start of the name
                    // when it has no dot). lastDot is initialized to 0 like in
                    // Dokan's DokanIsNameInExpression, which gives a bare "<"
                    // pattern the same (non-matching) semantics as the reference.
                    var lastDot = 0;
                    for (var i = 0; i < name.Length; i++)
                        if (name[i] == '.')
                            lastDot = i;

                    var ni = 0;
                    while (ni < lastDot)
                    {
                        if (IsMatch(name[ni..], expression))
                            return true;

                        ni++;
                    }

                    name = name[lastDot..];
                    continue;
                }
                case DosQm:
                {
                    // DOS_QM matches a single character, except that a dot which
                    // is not the last dot in the name is consumed without being
                    // matched (8.3 extension handling). Like the Dokan C
                    // reference, a DOS_QM cannot consume anything when the name
                    // is exhausted: the reference advances past the NUL
                    // terminator, which fails its final length check.
                    expression = expression[1..];
                    if (name.Length == 0)
                        return false;

                    if (name[0] != '.')
                    {
                        name = name[1..];
                    }
                    else
                    {
                        var p = 1;
                        while (p < name.Length && name[p] != '.') p++;

                        if (p < name.Length) name = name[1..];
                    }

                    continue;
                }
                case DosDot:
                {
                    // DOS_DOT matches a literal dot if one is present.
                    expression = expression[1..];
                    if (name.Length > 0 && name[0] == '.') name = name[1..];

                    continue;
                }
            }

            // Literal character or regular '?'.
            if (name.Length == 0)
                return false;

            if (e == '?' || char.ToUpperInvariant(e) == char.ToUpperInvariant(name[0]))
            {
                expression = expression[1..];
                name = name[1..];
                continue;
            }

            return false;
        }

        return name.Length == 0;
    }
}