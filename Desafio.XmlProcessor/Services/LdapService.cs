using Novell.Directory.Ldap;
using Desafio.XmlProcessor.Models;

namespace Desafio.XmlProcessor.Services;

public class LdapService (LdapConnection ldapConnection, LdapSettings ldapSettings)
{
    private readonly LdapConnection _ldapConnection = ldapConnection;
    private readonly LdapSettings _ldapSettings = ldapSettings;

    public async Task Connect()
    {
        try
        {
            await _ldapConnection.ConnectAsync(_ldapSettings.Host, _ldapSettings.Port);
            await _ldapConnection.BindAsync(LdapConnection.LdapV3, _ldapSettings.AdminUser, _ldapSettings.AdminPassword);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao conectar ao LDAP: {e.Message}");
            return;
        }
    }

    public async Task<bool> AddUser(User user)
    {
        await EnsureConnection();

        try
        {
            LdapAttributeSet attributes =
            [
                new LdapAttribute("objectClass", ["top", "person", "organizationalPerson", "inetOrgPerson"]),
                new LdapAttribute("uid", user.Uid),
                new LdapAttribute("cn", user.FullName),
                new LdapAttribute("sn", user.Surname),
                new LdapAttribute("telephoneNumber", user.Phone),
            ];

            LdapEntry newEntry = new(user.DistinguishedName, attributes);
            await AddContainerIfNotExists("users");
            var userExists = await UserExists(user.Uid);
            if (userExists)
                return false;
            await _ldapConnection.AddAsync(newEntry);
            Console.WriteLine($"Usuário '{user.Uid}' adicionado ao LDAP.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao adicionar usuário {user.Uid} ao LDAP: {ex.Message}");
            return false;
        }
    }

    public async Task AddGroup(Group group, List<User> users)
    {
        await EnsureConnection();

        try
        {
            string members = "";
            foreach (var user in users)
            {
                members += user.DistinguishedName + ",";
            }
            members = users.Count > 0 ? members[..^1] : "";

            LdapAttributeSet attributes =
            [
                new LdapAttribute("objectClass", ["top", "groupOfNames"]),
            new LdapAttribute("cn", group.Identifier),
            new LdapAttribute("description", group.Description),
            new LdapAttribute("member", members)
            ];

            LdapEntry newEntry = new(group.DistinguishedName, attributes);
            await AddContainerIfNotExists("groups");
            var groupExists = await GroupExists(group.Identifier);
            if (groupExists)
                return;
            await _ldapConnection.AddAsync(newEntry);
            Console.WriteLine($"Grupo '{group.Description}' adicionado ao LDAP.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao adicionar grupo {group.Identifier} ao LDAP: {ex.Message}");
            return;
        }
    }

    public async Task<List<Group>> GetGroups()
    {
        List<Group> groups = [];

        try
        {
            await EnsureConnection();

            var results = await _ldapConnection.SearchAsync(
                _ldapSettings.GroupsContainer,
                LdapConnection.ScopeSub,
                "(objectClass=groupOfNames)",
                null,
                false
            );

            while (await results.HasMoreAsync())
            {
                LdapEntry entry = await results.NextAsync();

                string identifier = entry.Get("cn").StringValue;
                string description = entry.Get("description").StringValue;
                var newGroup = new Group(identifier, description);

                LdapAttribute memberAttribute = entry.Get("member");
                if (memberAttribute is not null)
                {
                    string[] members = memberAttribute.StringValueArray;
                    foreach (var member in members)
                    {
                        if (string.IsNullOrEmpty(member))
                            continue;
                        newGroup.AddMember(member);
                    }
                }

                groups.Add(newGroup);
            }
        }
        catch (LdapException e)
        {
            if (e.ResultCode == LdapException.NoSuchObject)
            {
                Console.WriteLine($"Base '{_ldapSettings.GroupsContainer}' não encontrada. Retornando lista vazia.");
                return groups;
            }
            else
            {
                throw;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao buscar grupos: {e.Message}");
        }

        return groups;
    }

    public async Task<List<User>> GetUsers()
    {
        List<User> users = [];

        try
        {
            await EnsureConnection();

            var results = await _ldapConnection.SearchAsync(
                _ldapSettings.UsersContainer,
                LdapConnection.ScopeSub,
                "(objectClass=inetOrgPerson)",
                null,
                false
            );

            while (await results.HasMoreAsync())
            {
                LdapEntry entry = await results.NextAsync();

                string login = entry.Get("uid").StringValue;
                string fullName = entry.Get("cn").StringValue;
                string phone = entry.Get("telephoneNumber").StringValue;

                if (!string.IsNullOrEmpty(login) || !string.IsNullOrEmpty(fullName) || !string.IsNullOrEmpty(phone))
                    users.Add(new User(login, fullName, phone));
            }
        }
        catch (LdapException e)
        {
            if (e.ResultCode == LdapException.NoSuchObject)
            {
                Console.WriteLine($"Base '{_ldapSettings.UsersContainer}' não encontrada. Retornando lista vazia.");
                return users;
            }
            else
            {
                throw;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao buscar usuários: {e.Message}");
        }

        return users;
    }

    public async Task<bool> AddUserToGroup(User user, Group group)
    {
        await EnsureConnection();

        try
        {
            var results = await _ldapConnection.SearchAsync(
                _ldapSettings.GroupsContainer,
                LdapConnection.ScopeSub,
                $"(cn={group.Identifier})",
                null,
                false
            );
        
            LdapEntry groupEntry = await results.NextAsync();
            if (groupEntry is null)
            {
                Console.WriteLine($"Grupo '{group.Identifier}' não encontrado no LDAP.");
                return false;
            }

            if(await IsUserInGroup(user, group))
            {
                Console.WriteLine($"Usuário '{user.Uid}' já pertence ao grupo '{group.Identifier}'.");
                return false;
            }

            await _ldapConnection.ModifyAsync(
                groupEntry.Dn,
                new LdapModification(LdapModification.Add, new LdapAttribute("member", user.DistinguishedName))
            );

            Console.WriteLine($"Usuário '{user.Uid}' adicionado ao grupo '{group.Identifier}'.");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao adicionar usuário '{user.Uid}' ao grupo '{group.Identifier}': {e.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveUserFromGroup(User user, Group group)
    {
        await EnsureConnection();

        try
        {
            var results = await _ldapConnection.SearchAsync(
                _ldapSettings.GroupsContainer,
                LdapConnection.ScopeSub,
                $"(cn={group.Identifier})",
                null,
                false
            );

            LdapEntry groupEntry = await results.NextAsync();
            if (groupEntry is null)
            {
                Console.WriteLine($"Grupo '{group.Identifier}' não encontrado no LDAP.");
                return false;
            }

            if (!await IsUserInGroup(user, group))
            {
                Console.WriteLine($"Usuário '{user.Uid}' não pertence ao grupo '{group.Identifier}'.");
                return false;
            }

            await _ldapConnection.ModifyAsync(
                groupEntry.Dn,
                new LdapModification(LdapModification.Delete, new LdapAttribute("member", user.DistinguishedName))
            );

            Console.WriteLine($"Usuário '{user.Uid}' removido do grupo '{group.Identifier}'.");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao remover usuário '{user.Uid}' do grupo '{group.Identifier}': : {e.Message}");
            return false;
        }
    }

    public async Task<bool> IsUserInGroup(User user, Group group)
    {
        await EnsureConnection();
        
        try
        {
            var results = await _ldapConnection.SearchAsync(
                _ldapSettings.GroupsContainer,
                LdapConnection.ScopeSub,
                $"(cn={group.Identifier})",
                null,
                false
            );

            LdapEntry groupEntry = await results.NextAsync();
            if (groupEntry is null)
            {
                Console.WriteLine($"Grupo '{group.Identifier}' não encontrado no LDAP.");
                return false;
            }

            LdapAttribute memberAttribute = groupEntry.Get("member");
            if (memberAttribute is null)
            {
                Console.WriteLine($"Grupo '{group.Identifier}' não possui membros.");
                return false;
            }

            string[] members = memberAttribute.StringValueArray;
            return members.Any(member => member.Equals(user.DistinguishedName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao verificar se usuário '{user.Uid}' está no grupo '{group.Identifier}': {e.Message}");
            return false;
        }
    }

    public async Task ModifyUserGroups(string filePath)
    {
        await EnsureConnection();

        User user = new("", "", "");
        var modified = false;
        var (uid, groupsToRemove, groupsToAdd) = User.ModifyFromXml(filePath);

        try
        {
            var results = await _ldapConnection.SearchAsync(
                _ldapSettings.UsersContainer,
                LdapConnection.ScopeSub,
                $"(uid={uid})",
                null,
                false
            );

            LdapEntry userEntry = await results.NextAsync();
            if (userEntry is null)
            {
                Console.WriteLine($"Usuário '{uid}' não encontrado no LDAP.");
                return;
            }

            user = new User(
                uid,
                userEntry.Get("cn").StringValue,
                userEntry.Get("telephoneNumber").StringValue
            );

            foreach (var groupId in groupsToRemove)
            {
                var group = new Group(groupId, "");
                modified = await RemoveUserFromGroup(user, group);
            }

            foreach (var groupId in groupsToAdd)
            {
                var group = new Group(groupId, "");
                modified = await AddUserToGroup(user, group);
            }

            string message = modified ?
                $"Grupos do usuário '{user.Uid}' modificados com sucesso." :
                $"Nenhum grupo foi modificado para o usuário '{user.Uid}'.";

            Console.WriteLine(message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao modificar grupos do usuário '{user.Uid}': {e.Message}");
        }
    }

    private async Task EnsureConnection()
    {
        if (!_ldapConnection.Connected)
            await Connect();
    }

    private async Task AddContainerIfNotExists(string containerName)
    {
        try
        {
            await _ldapConnection.ReadAsync($"ou={containerName},{_ldapSettings.BaseDn}");
        }
        catch (LdapException)
        {
            var containerAttributes = new LdapAttributeSet
            {
                new LdapAttribute("objectClass", ["top", "organizationalUnit"]),
                new LdapAttribute("ou", containerName)
            };
            var containerEntry = new LdapEntry($"ou={containerName},{_ldapSettings.BaseDn}", containerAttributes);
            await _ldapConnection.AddAsync(containerEntry);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao adicionar container '{containerName}': {e.Message}");
            return;
        }
    }

    private async Task<bool> GroupExists(string groupName)
    {
        try
        {
            await _ldapConnection.ReadAsync($"cn={groupName},{_ldapSettings.GroupsContainer}");
            return true;
        }
        catch (LdapException)
        {
            return false;
        }
    }

    private async Task<bool> UserExists(string userId)
    {
        try
        {
            await _ldapConnection.ReadAsync($"uid={userId},{_ldapSettings.UsersContainer}");
            return true;
        }
        catch (LdapException)
        {
            return false;
        }
    }
}
