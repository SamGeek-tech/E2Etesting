using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

/// <summary>
/// Application service interface for identity/authentication operations.
/// </summary>
public interface IIdentityService
{
    LoginResponse Login(string email, string password);
}

