using System.Xml;
using System.Xml.Linq;
using Acms.Core.Domain;

namespace Acms.Infrastructure.Awacs;

/// <summary>
/// Turns a <c>wsdata.xml</c> response into <see cref="Workstation"/> objects.
/// Each workstation's XML attributes and leaf child elements become its attributes.
/// The exact AWACS layout is configurable (<see cref="AwacsOptions.WorkstationElementNames"/>,
/// <see cref="AwacsOptions.WorkstationIdNames"/>); see the unit tests for the layouts handled.
/// </summary>
public static class AwacsXmlParser
{
    public static IReadOnlyList<Workstation> Parse(string xml, AwacsOptions options, string? requestedWsId = null)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return [];
        }

        var root = Load(xml).Root;
        if (root is null)
        {
            return [];
        }

        var elements = FindWorkstationElements(root, options);
        var result = new List<Workstation>(elements.Count);

        foreach (var element in elements)
        {
            var attributes = ReadAttributes(element);
            var wsId = FindId(attributes, options.WorkstationIdNames)
                ?? (elements.Count == 1 ? requestedWsId : null);

            if (!string.IsNullOrWhiteSpace(wsId))
            {
                result.Add(new Workstation(wsId, attributes));
            }
        }

        return result;
    }

    private static XDocument Load(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
        };

        try
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, settings);
            return XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new AwacsException("AWACS returned a response that is not valid XML: " + ex.Message, ex);
        }
    }

    private static List<XElement> FindWorkstationElements(XElement root, AwacsOptions options)
    {
        var names = new HashSet<string>(options.WorkstationElementNames, StringComparer.OrdinalIgnoreCase);

        // 1. Elements with a configured name that carry data (not a leaf like <ws>A1</ws>).
        var named = root.DescendantsAndSelf()
            .Where(e => names.Contains(e.Name.LocalName) && (e.HasElements || e.HasAttributes))
            .ToList();

        if (named.Count > 0)
        {
            var selected = named.ToHashSet();
            return named.Where(e => !e.Ancestors().Any(selected.Contains)).ToList();
        }

        // 2. Root is a list: every direct child is a record with its own children.
        var children = root.Elements().ToList();
        if (children.Count > 0 && children.All(c => c.HasElements))
        {
            return children;
        }

        // 3. Root is the single workstation.
        return [root];
    }

    private static List<KeyValuePair<string, string>> ReadAttributes(XElement element)
    {
        var attributes = new List<KeyValuePair<string, string>>();

        foreach (var attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
        {
            attributes.Add(new(attribute.Name.LocalName, attribute.Value.Trim()));
        }

        foreach (var child in element.Elements().Where(c => !c.HasElements))
        {
            attributes.Add(new(child.Name.LocalName, child.Value.Trim()));
        }

        return attributes;
    }

    private static string? FindId(List<KeyValuePair<string, string>> attributes, string[] idNames)
    {
        foreach (var idName in idNames)
        {
            var match = attributes.FirstOrDefault(a => string.Equals(a.Key, idName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.Value))
            {
                return match.Value;
            }
        }

        return null;
    }
}

public sealed class AwacsException : Exception
{
    public AwacsException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
