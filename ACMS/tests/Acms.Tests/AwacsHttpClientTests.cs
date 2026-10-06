using System.Net;
using Acms.Core.Domain;
using Acms.Infrastructure.Awacs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Acms.Tests;

public class AwacsHttpClientTests
{
    private readonly StubHttpHandler _handler = new();
    private readonly AwacsOptions _options = new() { ReadRetryCount = 1 };
    private readonly AwacsServer _server = new() { Id = 1, Name = "MS079", BaseUrl = "http://myser01ms079.nws.nexperia.com/" };

    private AwacsHttpClient CreateClient() =>
        new(new HttpClient(_handler), Options.Create(_options), NullLogger<AwacsHttpClient>.Instance);

    [Fact]
    public async Task Reads_all_workstations_with_ws_star()
    {
        _handler.Enqueue(HttpStatusCode.OK, "<wsdata><ws><WSID>DB-AXF-013S</WSID><MODEL>XF_DBSG</MODEL></ws></wsdata>");

        var result = await CreateClient().GetWorkstationsAsync(_server, null);

        Assert.Equal("DB-AXF-013S", Assert.Single(result).WsId);
        Assert.Equal("http://myser01ms079.nws.nexperia.com/template/wsdata.xml?ws=*", _handler.Requests[0].ToString());
    }

    [Fact]
    public async Task Keeps_the_server_base_path()
    {
        _server.BaseUrl = "http://awacs01.company.local/awacs";

        await CreateClient().GetWorkstationsAsync(_server, null);

        Assert.Equal("/awacs/template/wsdata.xml", _handler.Requests[0].AbsolutePath);
    }

    [Fact]
    public async Task Escapes_each_workstation_id_but_keeps_the_comma_separator()
    {
        await CreateClient().GetWorkstationsAsync(_server, ["WS 1", "A&B"]);

        Assert.Equal("?ws=WS%201,A%26B", _handler.Requests[0].Query);
    }

    [Fact]
    public async Task Splits_long_workstation_lists_into_several_reads()
    {
        _options.MaxIdsPerRequest = 2;
        _handler.Enqueue(HttpStatusCode.OK, "<wsdata><ws><WSID>A</WSID><X>1</X></ws><ws><WSID>B</WSID><X>1</X></ws></wsdata>");
        _handler.Enqueue(HttpStatusCode.OK, "<wsdata><ws><WSID>C</WSID><X>1</X></ws></wsdata>");

        var result = await CreateClient().GetWorkstationsAsync(_server, ["A", "B", "C", "a"]);

        Assert.Equal(["?ws=A,B", "?ws=C"], _handler.Requests.Select(r => r.Query));
        Assert.Equal(["A", "B", "C"], result.Select(w => w.WsId));
    }

    [Fact]
    public async Task GetWorkstation_returns_null_for_the_empty_answer_to_an_unknown_id()
    {
        _handler.Enqueue(HttpStatusCode.OK, "<?xml version=\"1.0\"?>\n<wsdata></wsdata>");

        Assert.Null(await CreateClient().GetWorkstationAsync(_server, "DB-AXF-999S"));
    }

    [Fact]
    public async Task Retries_reads_on_server_errors()
    {
        _handler.Enqueue(HttpStatusCode.ServiceUnavailable);
        _handler.Enqueue(HttpStatusCode.OK, "<wsdata><ws><WSID>A</WSID><X>1</X></ws></wsdata>");

        var result = await CreateClient().GetWorkstationsAsync(_server, ["A"]);

        Assert.Equal(2, _handler.Requests.Count);
        Assert.Single(result);
    }

    [Fact]
    public async Task Throws_AwacsException_on_client_errors_without_retrying()
    {
        _handler.Enqueue(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<AwacsException>(() => CreateClient().GetWorkstationsAsync(_server, ["A"]));
        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Sends_all_changes_in_one_documented_setwsattr_call()
    {
        var result = await CreateClient().UpdateAttributesAsync(_server, "DB-AXF-013S", new Dictionary<string, string>
        {
            ["SPEED_SPEC"] = "46000",
            ["CONTROL"] = "ATX18II,Flex",
            ["SRCFILE"] = @"\\db-axf-013s\C$\Itec\Work\DB-AXF-013S.esm",
        });

        Assert.True(result.Accepted);
        var request = Assert.Single(_handler.Requests);
        Assert.Equal("/template/wswoupdate.html", request.AbsolutePath);

        var query = System.Web.HttpUtility.ParseQueryString(request.Query);
        Assert.Equal("DB-AXF-013S", query["ws"]);
        Assert.Equal(
            "WsId=\"DB-AXF-013S\",SPEED_SPEC=\"46000\",CONTROL=\"ATX18II,Flex\",SRCFILE=\"\\\\db-axf-013s\\C$\\Itec\\Work\\DB-AXF-013S.esm\"",
            query["setwsattr"]);
    }

    [Fact]
    public async Task Can_use_the_colon_format()
    {
        _options.UpdateAttributeFormat = AwacsAttributeFormat.Colon;

        await CreateClient().UpdateAttributesAsync(_server, "DB-AXF-013S", new Dictionary<string, string> { ["SPEED_SPEC"] = "46000" });

        var query = System.Web.HttpUtility.ParseQueryString(_handler.Requests[0].Query);
        Assert.Equal("WsId:DB-AXF-013S,SPEED_SPEC:46000", query["setwsattr"]);
    }

    [Theory]
    [InlineData(AwacsAttributeFormat.Quoted, "say \"hi\"")]
    [InlineData(AwacsAttributeFormat.Colon, "ATX18II,Flex")]
    public async Task Refuses_values_the_format_cannot_carry_without_calling_AWACS(AwacsAttributeFormat format, string value)
    {
        _options.UpdateAttributeFormat = format;

        var result = await CreateClient().UpdateAttributesAsync(_server, "A", new Dictionary<string, string> { ["CONTROL"] = value });

        Assert.False(result.Accepted);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Reports_a_rejected_update_and_never_retries_it()
    {
        _handler.Enqueue(HttpStatusCode.InternalServerError);

        var result = await CreateClient().UpdateAttributesAsync(_server, "A", new Dictionary<string, string> { ["X"] = "1", ["Y"] = "2" });

        Assert.False(result.Accepted);
        Assert.Contains("500", result.Detail);
        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Treats_a_failure_marker_in_a_200_response_as_rejected()
    {
        _options.UpdateFailureMarkers = ["ERROR"];
        _handler.Enqueue(HttpStatusCode.OK, "<html>Error: attribute is locked</html>");

        var result = await CreateClient().UpdateAttributesAsync(_server, "A", new Dictionary<string, string> { ["X"] = "1" });

        Assert.False(result.Accepted);
        Assert.Contains("attribute is locked", result.Detail);
    }
}
