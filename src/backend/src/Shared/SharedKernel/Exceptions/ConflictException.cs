using System;

namespace SharedKernel.Exceptions;

    public class ConflictException : AppException
    {
        public override int StatusCode => 409;
        public ConflictException(string message, string? code = null) : base(message, code) { }
    }
