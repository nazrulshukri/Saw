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
    private readonly AwacsServer _server = new() { Id = 1, Name = "AWACS-1", BaseUrl = "http://awacs01.company.local/awacs" };

    private AwacsHttpClient CreateClient() =>
        new(new HttpClient(_handler), Options.Create(_options), NullLogger<AwacsHttpClient>.Instance);

    [Fact]
    public async Task Reads_all_workstations_with_ws_star_under_the_server_base_path()
    {
        _handler.Enqueue(HttpStatusCode.OK, "<wsdata><ws id=\"A\"><STATE>IDLE</STATE></ws></wsdata>");

        var result = await CreateClient().GetWorkstationsAsync(_server, null);

        Assert.Equal("A", Assert.Single(result).WsId);
        Assert.Equal("http://awacs01.company.local/awacs/template/wsdata.xml?ws=*", _handler.Requests[0].ToString());
    }

    [Fact]
    public async Task Escapes_each_workstation_id_but_keeps_the_comma_separator()
    {
        await CreateClient().GetWorkstationsAsync(_server, ["WS 1", "A&B"]);

        Assert.Equal("?ws=WS%201,A%26B", _handler.Requests[0].Query);
    }

    [Fact]
    public async Task GetWorkstation_returns_null_when_AWACS_returns_a_different_id()
    {
        _handler.Enqueue(HttpStatusCode.OK, "<wsdata><ws id=\"OTHER\"><STATE>IDLE</STATE></ws></wsdata>");

        Assert.Null(await CreateClient().GetWorkstationAsync(_server, "RM-ELM-001"));
    }

    [Fact]
    public async Task Retries_reads_on_server_errors()
    {
        _handler.Enqueue(HttpStatusCode.ServiceUnavailable);
        _handler.Enqueue(HttpStatusCode.OK, "<wsdata><ws id=\"A\"/></wsdata>");

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
    public async Task Sends_one_encoded_update_per_attribute()
    {
        var result = await CreateClient().UpdateAttributesAsync(_server, "RM-ELM-001", new Dictionary<string, string>
        {
            ["TOP_LINE_1"] = "B7t,DB09,639, ",
            ["RECIPELOAD"] = "RCP_02",
        });

        Assert.True(result.Accepted);
        Assert.Equal(2, _handler.Requests.Count);
        Assert.Equal("/awacs/template/wswoupdate.html", _handler.Requests[0].AbsolutePath);
        Assert.Equal("?ws=RM-ELM-001&setwsattr=TOP_LINE_1&value=B7t%2CDB09%2C639%2C%20", _handler.Requests[0].Query);
        Assert.Equal("?ws=RM-ELM-001&setwsattr=RECIPELOAD&value=RCP_02", _handler.Requests[1].Query);
    }

    [Fact]
    public async Task Stops_at_the_first_rejected_update_and_never_retries_it()
    {
        _handler.Enqueue(HttpStatusCode.InternalServerError);

        var result = await CreateClient().UpdateAttributesAsync(_server, "A", new Dictionary<string, string>
        {
            ["X"] = "1",
            ["Y"] = "2",
        });

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
