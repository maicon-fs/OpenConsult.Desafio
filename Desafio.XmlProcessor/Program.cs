using Microsoft.Extensions.Configuration;
using Novell.Directory.Ldap;
using Desafio.XmlProcessor.Models;
using Desafio.XmlProcessor.Services;

namespace Desafio.XmlProcessor;
public partial class Program
{
    static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var ldapSettings = configuration.GetSection("LdapSettings").Get<LdapSettings>();

        LdapService ldapService = new(new LdapConnection(), ldapSettings ?? new LdapSettings());
        string inputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inputs");

        var filePathAddUsuario1 = Path.Combine(inputFolder, "AddUsuario1.xml");
        var filePathAddGrupo1 = Path.Combine(inputFolder, "AddGrupo1.xml");
        var filePathAddGrupo2 = Path.Combine(inputFolder, "AddGrupo2.xml");
        var filePathAddGrupo3 = Path.Combine(inputFolder, "AddGrupo3.xml");
        var filePathModifyUsuario = Path.Combine(inputFolder, "ModifyUsuario.xml");

        var newGroup1 = Group.GetFromXml(filePathAddGrupo1);
        await ldapService.AddGroup(newGroup1, []);

        var newGroup2 = Group.GetFromXml(filePathAddGrupo2);
        await ldapService.AddGroup(newGroup2, []);

        var newGroup3 = Group.GetFromXml(filePathAddGrupo3);
        await ldapService.AddGroup(newGroup3, []);

        var (newUser1, groups) = User.GetFromXml(filePathAddUsuario1);
        bool userRegistered = await ldapService.AddUser(newUser1);
        if (userRegistered)
        {
            foreach (var group in groups)
            {
                var newGroup = new Group(group, "");
                await ldapService.AddUserToGroup(newUser1, newGroup);
            }
        }

        await ldapService.ModifyUserGroups(filePathModifyUsuario);

        var _users = await ldapService.GetUsers();
        var _groups = await ldapService.GetGroups();

        Console.WriteLine("");

        foreach (var group in _groups)
        {
            Console.WriteLine($"Grupo: '{group.Identifier}' - '{group.Description}'");
            Console.WriteLine("Usuários:");
            foreach (var member in group.Members)
            {
                var user = _users.FirstOrDefault(u => u.DistinguishedName.Equals(member, StringComparison.OrdinalIgnoreCase));
                if (user is not null)
                {
                    Console.WriteLine($"'{user.FullName}' - '{user.Uid}' - '{user.Phone}'");
                }
            }
            Console.WriteLine("");
        }

        Console.WriteLine("Pressione qualquer tecla para sair...");
        Console.ReadKey();
    }
}