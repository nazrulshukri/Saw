using Acms.Infrastructure.Awacs;

namespace Acms.Tests;

public class AwacsXmlParserTests
{
    private static readonly AwacsOptions Options = new();

    [Fact]
    public void Reads_ws_elements_with_id_attribute_and_child_values()
    {
        const string xml = """
            <wsdata>
              <ws id="RM-ELM-001" type="L200">
                <RECIPELOAD> RCP_01 </RECIPELOAD>
                <STATE>IDLE</STATE>
              </ws>
              <ws id="RM-ELM-002">
                <RECIPELOAD>RCP_02</RECIPELOAD>
              </ws>
            </wsdata>
            """;

        var result = AwacsXmlParser.Parse(xml, Options);

        Assert.Equal(2, result.Count);
        Assert.Equal("RM-ELM-001", result[0].WsId);
        Assert.Equal("L200", result[0].Get("type"));
        Assert.Equal("RCP_01", result[0].Get("recipeload"));
        Assert.Equal("IDLE", result[0].Get("STATE"));
        Assert.Equal("RM-ELM-002", result[1].WsId);
    }

    [Fact]
    public void Matches_element_names_case_insensitively_and_reads_WSID_child()
    {
        const string xml = "<WSDATA><WS><WSID>AD45C1</WSID><RECIPELOAD>X</RECIPELOAD></WS></WSDATA>";

        var result = AwacsXmlParser.Parse(xml, Options);

        var ws = Assert.Single(result);
        Assert.Equal("AD45C1", ws.WsId);
        Assert.Equal("X", ws.Get("RECIPELOAD"));
    }

    [Fact]
    public void Treats_repeated_root_children_as_workstations_when_no_configured_name_matches()
    {
        const string xml = """
            <stations>
              <station><WSID>A</WSID><STATE>RUNNING</STATE></station>
              <station><WSID>B</WSID><STATE>IDLE</STATE></station>
            </stations>
            """;

        var result = AwacsXmlParser.Parse(xml, Options);

        Assert.Equal(["A", "B"], result.Select(w => w.WsId));
    }

    [Fact]
    public void Treats_flat_root_as_single_workstation_and_falls_back_to_requested_id()
    {
        const string xml = "<root><RECIPELOAD>RCP_9</RECIPELOAD><STATE>IDLE</STATE></root>";

        var result = AwacsXmlParser.Parse(xml, Options, requestedWsId: "DC-AD1-008");

        var ws = Assert.Single(result);
        Assert.Equal("DC-AD1-008", ws.WsId);
        Assert.Equal("RCP_9", ws.Get("RECIPELOAD"));
    }

    [Fact]
    public void Does_not_treat_a_leaf_named_ws_as_a_workstation()
    {
        const string xml = "<data><workstation id=\"A\"><ws>A</ws><STATE>IDLE</STATE></workstation></data>";

        var ws = Assert.Single(AwacsXmlParser.Parse(xml, Options));
        Assert.Equal("A", ws.WsId);
        Assert.Equal("A", ws.Get("ws"));
    }

    [Fact]
    public void Returns_empty_for_empty_response()
    {
        Assert.Empty(AwacsXmlParser.Parse("  ", Options));
        Assert.Empty(AwacsXmlParser.Parse("<wsdata/>", Options));
    }

    [Fact]
    public void Throws_AwacsException_for_html_error_pages()
    {
        Assert.Throws<AwacsException>(() => AwacsXmlParser.Parse("<html><body>Error<br></body></html>", Options));
    }

    [Fact]
    public void Rejects_DTDs()
    {
        const string xml = """
            <!DOCTYPE foo [ <!ENTITY xxe SYSTEM "file:///etc/passwd"> ]>
            <wsdata><ws id="A"><X>&xxe;</X></ws></wsdata>
            """;

        Assert.Throws<AwacsException>(() => AwacsXmlParser.Parse(xml, Options));
    }
}
