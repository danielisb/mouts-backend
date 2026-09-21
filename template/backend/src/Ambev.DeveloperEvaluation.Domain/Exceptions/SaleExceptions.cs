namespace Ambev.DeveloperEvaluation.Domain.Exceptions;

public class SaleNotFoundException : Exception
{
    public SaleNotFoundException(Guid id)
        : base($"Sale with ID {id} was not found.")
    {
    }
}

public class SaleConflictException : Exception
{
    public SaleConflictException(string message)
        : base(message)
    {
    }
}