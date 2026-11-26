namespace TodoApi.Domain.Exceptions;

/// <summary>
/// Representa a violação de uma regra de negócio do domínio.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
