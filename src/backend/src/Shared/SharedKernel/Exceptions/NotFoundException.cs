using System;

namespace SharedKernel.Exceptions;

    public class NotFoundException : AppException
    {
        public override int StatusCode => 404;
        public NotFoundException(string message, string? code = null) : base(message, code) { }
    }
