namespace DocsApi.Domain.Entities;

public enum AuthType
{
    Basic
}

public record ServiceAuth(AuthType Type, string Username, string Password);
