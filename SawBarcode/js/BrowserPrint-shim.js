// Minimal Zebra Browser Print client (replaces BrowserPrint-3.1.250.min.js).
// Talks to the Zebra Browser Print app running on the user's own PC (localhost:9100 / https 9101).
// Provides only what Saw.aspx uses: getLocalDevices, getDefaultDevice, device.send
(function (window) {
    var BASE = (window.location.protocol === "https:") ? "https://localhost:9101" : "http://127.0.0.1:9100";

    function request(method, path, body, ok, fail) {
        var x = new XMLHttpRequest();
        x.open(method, BASE + path, true);
        x.timeout = 5000;
        x.onload = function () {
            if (x.status >= 200 && x.status < 300) { if (ok) ok(x.responseText); }
            else if (fail) fail(x.responseText || ("HTTP " + x.status));
        };
        x.onerror = x.ontimeout = function () { if (fail) fail("Zebra Browser Print not reachable"); };
        if (body != null) x.setRequestHeader("Content-Type", "text/plain;charset=UTF-8"); // plain text: no CORS preflight
        x.send(body);
    }

    function Device(d) {
        for (var k in d) { if (d.hasOwnProperty(k)) this[k] = d[k]; }
    }
    Device.prototype.send = function (data, onSuccess, onError) {
        var self = this;
        var payload = {};
        for (var k in self) { if (self.hasOwnProperty(k)) payload[k] = self[k]; }
        request("POST", "/write", JSON.stringify({ device: payload, data: data }), onSuccess, onError);
    };

    window.BrowserPrint = {
        getLocalDevices: function (onSuccess, onError, type) {
            request("GET", "/available", null, function (text) {
                var list = [];
                try {
                    var json = JSON.parse(text);
                    var groups = type ? [json[type] || []] : [json.printer || []];
                    for (var g = 0; g < groups.length; g++)
                        for (var i = 0; i < groups[g].length; i++) list.push(new Device(groups[g][i]));
                } catch (e) { }
                onSuccess(list);
            }, onError);
        },
        getDefaultDevice: function (type, onSuccess, onError) {
            request("GET", "/default?type=" + encodeURIComponent(type || "printer"), null, function (text) {
                var dev = null;
                try { if (text && text.replace(/\s/g, "") !== "") dev = new Device(JSON.parse(text)); } catch (e) { }
                onSuccess(dev);
            }, onError);
        }
    };
})(window);
