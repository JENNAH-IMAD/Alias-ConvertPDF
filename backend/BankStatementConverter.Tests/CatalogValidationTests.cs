using BankStatementConverter.Application;
using BankStatementConverter.Infrastructure;
using Xunit;

namespace BankStatementConverter.Tests;

public class CatalogValidationTests
{
    [Theory]
    [InlineData("123")]
    [InlineData("12345678901234A")]
    [InlineData("")]
    public void InvalidIceIsRejected(string ice) => Assert.Throws<AppException>(() => Validation.Client(
        new ClientInput("Nom", "Société", ice, "IF", "RC", "a@example.test", "+212600000000")));

    [Fact]
    public void ValidClientIsAccepted() => Validation.Client(
        new ClientInput("Nom", "Société", "123456789012345", "IF", "RC", "a@example.test", "+212600000000"));

    [Theory]
    [InlineData("short")]
    [InlineData("withoutuppercase123")]
    [InlineData("WITHOUTLOWERCASE123")]
    [InlineData("WithoutAnyDigits")]
    public void WeakPasswordsAreRejected(string password) => Assert.Throws<AppException>(() => Validation.Password(password));
}
