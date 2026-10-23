namespace ModernCleanCode.Application.Errors;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);
