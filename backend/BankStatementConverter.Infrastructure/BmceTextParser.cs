using System.Globalization;
using System.Text.RegularExpressions;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
namespace BankStatementConverter.Infrastructure;
public class BmceTextParser : IBankStatementParser
{
    public string Key=>"bmce-text";
    public IReadOnlyList<BankTransaction> Parse(string text,BankStatementTemplate template)
    {
        const string money=@"(?:\d{1,3}(?:[ \u00a0\u202f]\d{3})+|\d+),\d{2}";
        var pattern=new Regex(@"^(?<date>\d{2}/\d{2}/\d{4})\s+(?<label>.+?)\s+(?<debit>"+money+@")\s+(?<credit>"+money+@")$");
        var rows=new List<BankTransaction>();bool table=false;
        foreach(var raw in text.Replace("\r","").Split('\n')){
            var line=raw.Trim();if(line.Length==0)continue;
            if(Regex.IsMatch(line,@"^Date\s+Libellé\s+Débit.*Crédit",RegexOptions.IgnoreCase)){table=true;continue;}
            if(!table)continue;
            if(line=="Les informations et opérations de ce document sont entièrement fictives.")continue;
            var match=pattern.Match(line);Validation.Require(match.Success,"Ligne de tableau BMCE texte non reconnue : aucun export partiel généré.");
            Validation.Require(DateOnly.TryParseExact(match.Groups["date"].Value,"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date),"Date BMCE invalide.");
            var debit=AmountNormalizer.Parse(match.Groups["debit"].Value,"fr-FR");var credit=AmountNormalizer.Parse(match.Groups["credit"].Value,"fr-FR");
            Validation.Require((debit>0)!=(credit>0),"Opération BMCE ambiguë : un seul débit ou crédit positif attendu.");
            rows.Add(new(){Date=date,Description=match.Groups["label"].Value,Debit=debit,Credit=credit});
        }
        Validation.Require(rows.Count>0,"Tableau BMCE texte introuvable. Structure attendue : Date, Libellé, Débit, Crédit.");
        return rows;
    }
}
public class BmceAutomaticParser : IBankStatementParser
{
    public string Key=>"bmce-auto";
    public IReadOnlyList<BankTransaction> Parse(string text,BankStatementTemplate template)=>text.TrimStart().StartsWith('[')?new BmceStatementParser().Parse(text,template):new BmceTextParser().Parse(text,template);
}
