namespace ComputerStore.Application.Exceptions;

public sealed class AccountOperationException : Exception
{
    public int StatusCode { get; }
    public AccountOperationException(string message, int statusCode = 400) : base(message) => StatusCode = statusCode;
}

public sealed class OrderOperationException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public enum ProductErrorKind
    {
        InvalidInput,
        NotFound,
        Conflict
    }

    public sealed class ProductOperationException : Exception
    {
        public ProductErrorKind Kind { get; }

        public ProductOperationException(ProductErrorKind kind, string message) : base(message)
        {
            Kind = kind;
        }
    }
