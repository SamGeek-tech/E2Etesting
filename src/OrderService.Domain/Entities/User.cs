namespace OrderService.Domain.Entities;

/// <summary>
/// Represents a user for authentication purposes.
/// In a real application, this would be in a separate Identity bounded context.
/// </summary>
public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // EF Core requires parameterless constructor
    private User() { }

    public User(string email, string passwordHash, string name)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        Email = email;
        PasswordHash = passwordHash;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public bool ValidatePassword(string passwordHash)
    {
        return PasswordHash == passwordHash;
    }
}

