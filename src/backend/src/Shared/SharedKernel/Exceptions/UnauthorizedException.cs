using System;

namespace SharedKernel.Exceptions;

    public class UnauthorizedException : AppException
    {
        public override int StatusCode => 401;
        public UnauthorizedException(string message, string? code = null) : base(message, code) { }
    }
