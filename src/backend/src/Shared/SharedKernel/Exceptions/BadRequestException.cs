using System;

namespace SharedKernel.Exceptions;

    public class BadRequestException : AppException
    {
        public override int StatusCode => 400;
        public BadRequestException(string message, string? code = null) : base(message, code) { }
    }
