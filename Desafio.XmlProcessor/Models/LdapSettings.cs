namespace Desafio.XmlProcessor.Models;

public class LdapSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string AdminPassword { get; set; } = string.Empty;
    public string BaseDn { get; set; } = string.Empty;
    public string AdminUser { get; set; } = string.Empty;
    public string UsersContainer { get; set; } = string.Empty;
    public string GroupsContainer { get; set; } = string.Empty;
}

