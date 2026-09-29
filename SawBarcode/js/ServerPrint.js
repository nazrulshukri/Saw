// Server-side printing client (used when Web.config PrintMode = "server").
// Same small API as BrowserPrint-shim.js, but the printers are the ones installed on the WEB SERVER
// and labels are sent to them by PrintService.ashx. No software is needed on the user's PC.
(function (window) {
    var URL = "PrintService.ashx";

    function errorText(x) {
        try { var j = JSON.parse(x.responseText); if (j && j.error) return j.error; } catch (e) { }
        return x.responseText || ("HTTP " + x.status);
    }

    function request(method, query, body, ok, fail) {
        var x = new XMLHttpRequest();
        x.open(method, URL + query, true);
        x.timeout = 15000;
        x.onload = function () {
            if (x.status >= 200 && x.status < 300) { if (ok) ok(x.responseText); }
            else if (fail) fail(errorText(x));
        };
        x.onerror = x.ontimeout = function () { if (fail) fail("Print service on the server is not reachable"); };
        if (body != null) x.setRequestHeader("Content-Type", "text/plain;charset=UTF-8");
        x.send(body);
    }

    function Device(name) {
        this.uid = name;
        this.name = name;
        this.connection = "server";
        this.deviceType = "printer";
    }
    Device.prototype.send = function (data, onSuccess, onError) {
        request("POST", "?action=print&printer=" + encodeURIComponent(this.name), data,
            function () { if (onSuccess) onSuccess(); }, onError);
    };

    function loadList(onSuccess, onError) {
        request("GET", "?action=list", null, function (text) {
            var info = { printers: [], "default": "" };
            try { info = JSON.parse(text); } catch (e) { }
            onSuccess(info);
        }, onError);
    }

    window.BrowserPrint = {
        getLocalDevices: function (onSuccess, onError, type) {
            loadList(function (info) {
                var list = [];
                for (var i = 0; i < info.printers.length; i++) list.push(new Device(info.printers[i]));
                onSuccess(list);
            }, onError);
        },
        getDefaultDevice: function (type, onSuccess, onError) {
            loadList(function (info) {
                onSuccess(info["default"] ? new Device(info["default"]) : null);
            }, onError);
        }
    };
})(window);
