/*
 * zebra-print.js - print raw ZPL to a local Zebra printer WITHOUT Java.
 *
 * Replaces the old jZebra applet (js/jzebra.jar). It talks to "Zebra Browser Print",
 * a small free service installed on every operator PC that listens on
 * http://localhost:9100 (https://localhost:9101). No Zebra SDK file is needed.
 *
 *   ZebraPrint.printerName        printer to look for (substring match), default "ZDesigner GX430t"
 *   ZebraPrint.detect(callback)   async; callback({ ok, message }) - used for the status bar
 *   ZebraPrint.print(zpl)         SYNCHRONOUS; returns { ok, message }
 *
 * print() is deliberately synchronous, like the old applet call: "Print All" prints from an
 * onclick and then posts back to the server straight away, and a normal (async) request
 * would be cancelled by that page reload.
 */
var ZebraPrint = (function () {
    "use strict";

    var self = {
        printerName: "ZDesigner GX430t"
    };

    var printer = null;      // device object last returned by Browser Print
    var activeBase = null;   // service URL that answered last time

    function bases() {
        var http = "http://localhost:9100/";
        var https = "https://localhost:9101/";
        if (activeBase) return [activeBase];
        return window.location.protocol === "https:" ? [https, http] : [http, https];
    }

    // One HTTP call. cb(status, responseText); status 0 = service not reachable.
    // "text/plain" is a CORS-safelisted type, so the browser sends no preflight request.
    function call(method, url, body, isAsync, cb) {
        var done = false;
        function finish(status, text) {
            if (done) return;
            done = true;
            cb(status, text);
        }
        try {
            var req = new XMLHttpRequest();
            req.open(method, url, isAsync);
            if (body !== null) req.setRequestHeader("Content-Type", "text/plain;charset=UTF-8");
            if (isAsync) {
                req.timeout = 5000;
                req.onreadystatechange = function () {
                    if (req.readyState === 4) finish(req.status, req.responseText);
                };
            }
            req.send(body);
            if (!isAsync) finish(req.status, req.responseText);
        } catch (e) {
            finish(0, String(e));
        }
    }

    function parsePrinters(text) {
        try {
            var data = JSON.parse(text);
            return (data && data.printer) ? data.printer : [];
        } catch (e) {
            return [];
        }
    }

    function names(list) {
        var n = [];
        for (var i = 0; i < list.length; i++) n.push(list[i].name);
        return n.join(", ");
    }

    // Pick our printer: name match first; if the name is not found but Browser Print
    // knows exactly ONE printer, use that one. Never guess between several.
    function choose(list) {
        var wanted = self.printerName.toLowerCase();
        var i;
        for (i = 0; i < list.length; i++) {
            if (list[i].name && list[i].name.toLowerCase().indexOf(wanted) >= 0) return list[i];
        }
        return list.length === 1 ? list[0] : null;
    }

    // Ask Browser Print for the printer list, trying each service URL in turn.
    // cb({ ok, printer, message })
    function lookup(isAsync, cb) {
        var candidates = bases();
        var index = 0;

        function tryNext() {
            if (index >= candidates.length) {
                printer = null;
                activeBase = null;
                cb({ ok: false, printer: null, message: "Zebra Browser Print not running or not installed" });
                return;
            }
            var base = candidates[index++];
            call("GET", base + "available", null, isAsync, function (status, text) {
                if (status < 200 || status >= 300) { tryNext(); return; }
                activeBase = base;
                var list = parsePrinters(text);
                var found = choose(list);
                if (found) {
                    printer = found;
                    cb({ ok: true, printer: found, message: "Printer \"" + found.name + "\" is ready" });
                } else {
                    printer = null;
                    cb({
                        ok: false, printer: null,
                        message: list.length === 0
                            ? "Printer Not Ready (Browser Print sees no printer)"
                            : "Printer \"" + self.printerName + "\" not found. Browser Print has: " + names(list)
                    });
                }
            });
        }
        tryNext();
    }

    self.detect = function (callback) {
        lookup(true, callback);
    };

    self.print = function (zpl) {
        var result = null;
        if (!printer) {
            lookup(false, function (r) { result = r; });
            if (!result.ok) return { ok: false, message: result.message };
        }

        var payload = JSON.stringify({ device: printer, data: zpl });
        call("POST", activeBase + "write", payload, false, function (status, text) {
            if (status >= 200 && status < 300) {
                result = { ok: true, message: "Printed Successfully" };
            } else {
                printer = null;      // look the printer up again next time
                activeBase = null;
                result = {
                    ok: false,
                    message: status === 0
                        ? "Error: Zebra Browser Print not reachable"
                        : "Error: Browser Print returned " + status + (text ? " " + text : "")
                };
            }
        });
        return result;
    };

    return self;
})();
