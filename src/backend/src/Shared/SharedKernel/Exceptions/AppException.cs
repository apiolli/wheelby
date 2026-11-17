using System;

namespace SharedKernel.Exceptions;

    public abstract class AppException : Exception
    {
        public abstract int StatusCode { get; }

        // Código estable y legible por máquina (p. ej. "vehicle.overlap") que viaja
        // en problem+json como extensions.code. El cliente decide con el código,
        // nunca con el texto en español del mensaje. Si no se indica, el manejador
        // global usa uno genérico según el estado HTTP.
        public string? Code { get; }

        protected AppException(string message, string? code = null) : base(message)
        {
            Code = code;
        }
    }
