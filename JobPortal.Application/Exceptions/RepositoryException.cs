namespace JobPortal.Application.Exceptions
{
    // Translates infrastructure-specific failures (SqlException, DbUpdateException)
    // into a clean, layer-agnostic exception so Web/Controllers never need to
    // reference EF Core or SqlClient types directly.
    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }

        public RepositoryException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}