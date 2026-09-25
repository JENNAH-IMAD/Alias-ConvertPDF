using System.Globalization;
using System.Text.RegularExpressions;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
namespace BankStatementConverter.Infrastructure;

public sealed class ProfileTransactionExtractor
{
    public static decimal? Amount(string text, BankStatementProfile profile)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var normalized = text.Trim().Replace('\u00a0', ' ').Replace('\u202f', ' ');
        if (profile.ThousandsSeparator.Length > 0) normalized = normalized.Replace(profile.ThousandsSeparator, "");
        normalized = normalized.Replace(profile.DecimalSeparator, ".");
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
    public List<BankTransaction> Extract(string text, BankStatementProfile profile, string currency, decimal? confidence, CancellationToken ct = default)
    {
        var pattern = new Regex(profile.TransactionPattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        var result = new List<BankTransaction>();
        foreach (var line in text.Split('\n'))
        {
            ct.ThrowIfCancellationRequested();
            var match = pattern.Match(line.Trim());
            if (!match.Success) continue;
            DateOnly? Date(string group) => DateOnly.TryParseExact(match.Groups[group].Value, profile.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
            result.Add(new() { Position = result.Count + 1, TransactionDate = Date("date"), ValueDate = Date("valueDate"),
                Reference = match.Groups["reference"].Value, Description = match.Groups["description"].Value,
                Debit = Amount(match.Groups["debit"].Value, profile), Credit = Amount(match.Groups["credit"].Value, profile), Balance = Amount(match.Groups["balance"].Value, profile),
                Currency = currency, RawText = line[..Math.Min(line.Length, 2000)], ConfidenceScore = confidence });
            if (result.Count > 10000) throw new AppException(422, "Maximum 10 000 opérations par relevé.");
        }
        return result;
    }
}
