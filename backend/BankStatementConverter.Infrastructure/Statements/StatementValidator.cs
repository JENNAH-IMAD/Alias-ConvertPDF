using BankStatementConverter.Application;
using BankStatementConverter.Domain;
namespace BankStatementConverter.Infrastructure;

public sealed class StatementValidator : IStatementValidator
{
    public StatementCheck Check(BankStatement s)
    {
        var issues = new List<ValidationIssue>();
        if (s.Transactions.Count == 0) issues.Add(new("EMPTY", "Ajoutez au moins une opération."));
        if (s.PeriodStart is null || s.PeriodEnd is null || s.PeriodStart > s.PeriodEnd) issues.Add(new("PERIOD", "Renseignez une période valide."));
        if (s.OpeningBalance is null || s.ClosingBalance is null) issues.Add(new("BALANCES", "Renseignez les soldes initial et final."));
        var seen = new HashSet<string>();
        foreach (var t in s.Transactions.OrderBy(t => t.Position))
        {
            if (t.TransactionDate is null) issues.Add(new("DATE", "Date obligatoire.", t.Position));
            if (t.TransactionDate < s.PeriodStart || t.TransactionDate > s.PeriodEnd) issues.Add(new("PERIOD", "Opération hors période.", t.Position));
            if (string.IsNullOrWhiteSpace(t.Description)) issues.Add(new("DESCRIPTION", "Libellé obligatoire.", t.Position));
            if ((t.Debit ?? 0) < 0 || (t.Credit ?? 0) < 0 || ((t.Debit ?? 0) > 0) == ((t.Credit ?? 0) > 0)) issues.Add(new("AMOUNT", "Un seul montant positif : débit ou crédit.", t.Position));
            if (t.Currency != s.Currency) issues.Add(new("CURRENCY", "Devise incompatible.", t.Position));
            if (!seen.Add($"{t.TransactionDate}|{t.Reference}|{t.Description}|{t.Debit}|{t.Credit}")) issues.Add(new("DUPLICATE", "Opération identique : vérifiez le doublon.", t.Position));
        }
        var debit = s.Transactions.Sum(t => t.Debit ?? 0); var credit = s.Transactions.Sum(t => t.Credit ?? 0);
        var calculated = s.OpeningBalance + credit - debit; var difference = calculated - s.ClosingBalance;
        if (difference is not null && Math.Abs(difference.Value) > 0.01m) issues.Add(new("BALANCE_MISMATCH", "Le solde calculé ne correspond pas au solde final."));
        return new(debit, credit, calculated, difference, issues);
    }
}
