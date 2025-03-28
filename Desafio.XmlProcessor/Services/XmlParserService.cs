using System.Xml;

namespace Desafio.XmlProcessor.Services;

public class XmlParserService
{
    private readonly XmlDocument _xmlDoc;

    public XmlParserService(string filePath)
    {
        _xmlDoc = new XmlDocument();
        _xmlDoc.Load(filePath);
    }

    public string GetNodeValue(string xpath)
    {
        XmlNode? node = _xmlDoc.SelectSingleNode(xpath);
        return node?.InnerText ?? string.Empty;
    }

    public List<string> GetMultipleValues(string xpath)
    {
        XmlNodeList? nodes = _xmlDoc.SelectNodes(xpath);
        List<string> values = [];

        if (nodes is null)
            return values;

        foreach (XmlNode node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.InnerText))
                values.Add(node.InnerText.Trim());
        }

        return values;
    }
}
