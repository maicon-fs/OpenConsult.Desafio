using Desafio.XmlProcessor.Services;
using Desafio.XmlProcessor.Utilities;

namespace Desafio.XmlProcessor.Models;

public class User(string uid, string fullName, string phone)
{
    private readonly string _uid = uid ?? throw new ArgumentNullException(nameof(uid));
    private readonly string _fullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
    private readonly string _surname = fullName.Split(' ').Last();
    private readonly string _phone = phone ?? throw new ArgumentNullException(nameof(phone));

    public string Uid => _uid;
    public string FullName => _fullName;
    public string Surname => _surname;
    public string Phone => _phone;

    public string DistinguishedName => $"uid={_uid},ou=users,dc=desafio,dc=ldap";

    public static (User user, List<string> groups) GetFromXml(string filePath)
    {
        XmlParserService xmlParserService = new(filePath);

        string uid = xmlParserService.GetNodeValue("//add-attr[@attr-name='Login']/value");
        string fullName = xmlParserService.GetNodeValue("//add-attr[@attr-name='Nome Completo']/value");
        string phone = xmlParserService.GetNodeValue("//add-attr[@attr-name='Telefone']/value");
        phone = RegexValidator.NormalizePhone(phone);
        fullName = RegexValidator.NormalizeText(fullName);
        uid = RegexValidator.NormalizeText(uid);
        
        List<string> groups = xmlParserService.GetMultipleValues("//add-attr[@attr-name='Grupo']/value");
        
        return (new User(uid, fullName, phone), groups);
    }

    public static (string uid, List<string> groupsToRemove, List<string> groupsToAdd) ModifyFromXml(string filePath)
    {
        XmlParserService xmlParserService = new(filePath);

        string uid = xmlParserService.GetNodeValue("//association");
        List<string> groupsToRemove = xmlParserService.GetMultipleValues("//remove-value/value");
        List<string> groupsToAdd = xmlParserService.GetMultipleValues("//add-value/value");

        return (uid, groupsToRemove, groupsToAdd);
    }
}