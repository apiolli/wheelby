using Documents.Application.Common;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Documents.Application.Features.UploadDocument;

// RD-07: forma de la subida. El límite de tamaño es configurable.
internal sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator(IOptions<DocumentsOptions> options)
    {
        var maxBytes = options.Value.MaxFileSizeBytes;
        var maxMb = maxBytes / (1024d * 1024d);

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("El archivo está vacío.")
            .LessThanOrEqualTo(maxBytes)
            .WithMessage($"El archivo supera el tamaño máximo de {maxMb:0.##} MB.");
    }
}
