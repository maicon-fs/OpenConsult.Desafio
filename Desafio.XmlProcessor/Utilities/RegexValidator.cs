using System.Text.RegularExpressions;

namespace Desafio.XmlProcessor.Utilities;

partial class RegexValidator
{
    public static string NormalizePhone(string phone)
    {
        return PhoneNormalizer().Replace(phone, "");
    }

    public static string NormalizeText(string text)
    {
        return TextNormalizer().Replace(text, "");
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex PhoneNormalizer();

    [GeneratedRegex(@"[^a-zA-Z0-9\s]")]
    private static partial Regex TextNormalizer();
}