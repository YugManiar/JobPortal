namespace JobPortal.Application.Exceptions
{
    // Thrown when a candidate attempts to apply to a job they've already applied to.
    // Distinct from RepositoryException so the controller can show a specific,
    // user-friendly message instead of a generic "something went wrong."
    public class DuplicateApplicationException : RepositoryException
    {
        public DuplicateApplicationException(string message) : base(message) { }
    }
}