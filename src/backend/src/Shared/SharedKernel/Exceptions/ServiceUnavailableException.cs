using System;

namespace SharedKernel.Exceptions;

    // 503: un servicio externo del que depende la operación no está disponible.
    public class ServiceUnavailableException : AppException
    {
        public override int StatusCode => 503;
        public ServiceUnavailableException(string message) : base(message) { }
    }
