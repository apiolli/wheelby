namespace AccessControl.Application.Abstractions;

// Value viaja en el correo; Hash es lo único que se guarda.
public sealed record GeneratedToken(string Value, string Hash);

public interface IOneTimeTokenGenerator
{
    GeneratedToken Generate();
    string Hash(string token);
}