using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using BankStatementConverter.Application;
namespace BankStatementConverter.Infrastructure;

public static class Validation
{
    public static void Require(bool condition, string message) { if (!condition) throw new AppException(400, message); }
    public static void Text(string? value, string name, int max = 200) => Require(!string.IsNullOrWhiteSpace(value) && value.Length <= max, $"{name} obligatoire (maximum {max} caractères).");
    public static void Email(string? value) => Require(value is not null && value.Length <= 254 && new EmailAddressAttribute().IsValid(value), "Adresse e-mail invalide.");
    public static void Password(string? value) => Require(value is not null && value.Length is >= 12 and <= 128 && value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit), "Mot de passe : 12 à 128 caractères, majuscule, minuscule et chiffre requis.");
    public static void Client(ClientInput x)
    {
        Text(x.Name, "Nom"); Text(x.LegalName, "Raison sociale");
        Require(x.ICE is not null && Regex.IsMatch(x.ICE, @"^\d{15}$"), "ICE : 15 chiffres requis.");
        Text(x.IF, "IF", 30); Text(x.RC, "RC", 40); Email(x.Email);
        Require(x.Phone is not null && Regex.IsMatch(x.Phone, @"^\+?[\d ()-]{6,30}$"), "Téléphone invalide."); Text(x.Country, "Pays", 80);
    }
}
