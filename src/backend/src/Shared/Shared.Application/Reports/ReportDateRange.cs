using System.Globalization;
using FluentValidation;

namespace Shared.Application.Reports;

// RF-REP-05: rango de fechas de los reportes. from y to llegan como texto yyyy-MM-dd
// para que un formato inválido sea un 400 con el error por campo (RD-07), no un 400
// genérico del enlazado. Se interpretan en UTC: [from 00:00, to 00:00).
public sealed record ReportDateRange(DateOnly From, DateOnly To)
{
    public const string Format = "yyyy-MM-dd";

    public DateTime StartUtc => From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    public DateTime EndUtc => To.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public static bool TryParseDate(string? value, out DateOnly date)
        => DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    // Solo después de validar con AddReportRangeRules.
    public static ReportDateRange Parse(string? from, string? to)
    {
        if (!TryParseDate(from, out var start) || !TryParseDate(to, out var end))
            throw new ArgumentException("El rango de fechas no es válido.");

        return new ReportDateRange(start, end);
    }
}

public static class ReportDateRangeRules
{
    public static void AddReportRangeRules<T>(
        this AbstractValidator<T> validator, Func<T, string?> from, Func<T, string?> to)
    {
        validator.RuleFor(x => from(x))
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("from es obligatorio (yyyy-MM-dd).")
            .Must(v => ReportDateRange.TryParseDate(v, out _)).WithMessage("from debe tener el formato yyyy-MM-dd.")
            .OverridePropertyName("from");

        validator.RuleFor(x => to(x))
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("to es obligatorio (yyyy-MM-dd).")
            .Must(v => ReportDateRange.TryParseDate(v, out _)).WithMessage("to debe tener el formato yyyy-MM-dd.")
            .Must((x, v) => !ReportDateRange.TryParseDate(from(x), out var start)
                            || !ReportDateRange.TryParseDate(v, out var end)
                            || start < end)
            .WithMessage("from debe ser anterior a to.")
            .OverridePropertyName("to");
    }
}
