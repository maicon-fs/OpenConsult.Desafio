using Desafio.XmlProcessor.Services;

namespace Desafio.XmlProcessor.Models;

public class Group(string identifier, string description)
{
    private readonly string _identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
    private readonly string _description = description ?? throw new ArgumentNullException(nameof(description));
    private readonly List<string> _members = [];

    public string Identifier => _identifier;
    public string Description => _description;
    public List<string> Members => _members;
    public string DistinguishedName => $"cn={_identifier},ou=groups,dc=desafio,dc=ldap";

    public static Group GetFromXml(string filePath)
    {
        XmlParserService xmlParserService = new(filePath);
        string identifier = xmlParserService.GetNodeValue("//add-attr[@attr-name='Identificador']/value");
        string description = xmlParserService.GetNodeValue("//add-attr[@attr-name='Descricao']/value");

        return new Group(identifier, description);
    }

    public void AddMember(string member)
    {
        _members.Add(member);
    }
    
    public void RemoveMember(string member)
    {
        _members.Remove(member);
    }
}