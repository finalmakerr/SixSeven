#target photoshop

/*
Export / Update layer positions for Unity modular setup (MERGE mode)
-------------------------------------------------------------------
✅ Exports visible art bounds in PIXELS
✅ Can export SELECTED layers only (fast updates)
✅ Merges into existing JSON by layerName (filename-style key)
✅ Regenerates one clean CSV from merged data
✅ Saves next to PSD (same 2 files every time)
✅ Older Photoshop-safe (no JSON.stringify / JSON.parse required)

IMPORTANT
- This version uses layerName as the unique key (because you said you'll name layers exactly like filenames).
- If two layers have the SAME name in different folders, they will overwrite each other in the JSON.
  (If that happens later, we can switch key back to fullPath.)
*/

(function () {
    if (!app.documents.length) {
        alert("No document open.");
        return;
    }

    var doc = app.activeDocument;
    var oldUnits = app.preferences.rulerUnits;
    app.preferences.rulerUnits = Units.PIXELS;

    try {
        var docName = doc.name.replace(/\.[^\.]+$/, "");
        var docPath = (doc.path ? doc.path.fsName : Folder.desktop.fsName);

        // ---- ONE critical prompt only ----
        // 1 = Selected visible layers only (recommended fast updates)
        // 2 = All visible layers
        var modeInput = prompt(
            "Export mode:\n" +
            "1 = Selected visible layers only (FAST updates)\n" +
            "2 = All visible layers\n\n" +
            "Enter 1 or 2:",
            "1"
        );
        if (modeInput === null) return;

        var mode = String(modeInput).replace(/\s+/g, "");
        if (mode !== "1" && mode !== "2") {
            alert("Invalid mode. Use 1 or 2.");
            return;
        }

        var skipHidden = true;
        var layers = [];

        if (mode === "1") {
            layers = getSelectedLayersSafe(doc);

            // Fallback if Photoshop version can't read selected layers reliably
            if (!layers.length) {
                var fallback = confirm(
                    "No selected layers detected.\n\n" +
                    "Press OK to export ALL visible layers instead.\n" +
                    "Press Cancel to stop."
                );
                if (!fallback) return;
                collectLayersRecursive(doc.layers, layers, skipHidden);
            }
        } else {
            collectLayersRecursive(doc.layers, layers, skipHidden);
        }

        var docW = px(doc.width);
        var docH = px(doc.height);
        var docCenterX = docW / 2;
        var docCenterY = docH / 2;

        // Build rows from this run
        var runRows = [];
        for (var i = 0; i < layers.length; i++) {
            var lyr = layers[i];
            try {
                if (skipHidden && !lyr.visible) continue;
                if (lyr.typename !== "ArtLayer" && lyr.typename !== "LayerSet") continue;

                // Skip obvious non-art layers (adjustment/text/etc) unless they have real bounds
                if (lyr.typename === "ArtLayer") {
                    try {
                        // Many PS versions expose numeric kind codes; text/adjustments often fail bounds anyway.
                        // We'll rely on bounds try/catch as the real filter.
                    } catch (kindErr) {}
                }

                var b = lyr.bounds;
                var x1 = px(b[0]);
                var y1 = px(b[1]);
                var x2 = px(b[2]);
                var y2 = px(b[3]);

                var w = x2 - x1;
                var h = y2 - y1;
                if (w <= 0 || h <= 0) continue;

                var cx = x1 + (w / 2);
                var cy = y1 + (h / 2);

                var row = {
                    layerName: lyr.name,                 // ← your filename key
                    fullPath: getLayerPath(lyr),         // kept for debugging/reference
                    kind: (lyr.typename === "LayerSet") ? "Group" : safeLayerKind(lyr),
                    visible: !!lyr.visible,
                    x: round3(x1),
                    y: round3(y1),
                    w: round3(w),
                    h: round3(h),
                    centerX: round3(cx),
                    centerY: round3(cy),
                    offsetFromDocCenterX: round3(cx - docCenterX),
                    offsetFromDocCenterY: round3(cy - docCenterY),
                    docW: round3(docW),
                    docH: round3(docH)
                };

                runRows.push(row);

            } catch (eLayer) {
                // non-exportable layer (empty/adjustment/etc)
            }
        }

        if (!runRows.length) {
            alert("No exportable layers found in this run.");
            return;
        }

        var jsonFile = new File(docPath + "/" + docName + "_layer_positions.json");
        var csvFile  = new File(docPath + "/" + docName + "_layer_positions.csv");

        var existingPayload = readExistingJson(jsonFile);
        if (!existingPayload || !existingPayload.layers) {
            existingPayload = {
                document: {
                    name: doc.name,
                    width: round3(docW),
                    height: round3(docH)
                },
                count: 0,
                updatedAtUTC: "",
                layers: []
            };
        }

        // Merge by layerName (your filename naming)
        var mergeInfo = { added: 0, updated: 0 };
        var mergedLayers = mergeByLayerName(existingPayload.layers, runRows, mergeInfo);

        // Sort stable by layerName
        mergedLayers.sort(function (a, b) {
            var A = String(a.layerName).toLowerCase();
            var B = String(b.layerName).toLowerCase();
            if (A < B) return -1;
            if (A > B) return 1;
            return 0;
        });

        // Reindex
        for (var m = 0; m < mergedLayers.length; m++) {
            mergedLayers[m].index = m;
        }

        var payload = {
            document: {
                name: doc.name,
                width: round3(docW),
                height: round3(docH)
            },
            count: mergedLayers.length,
            updatedAtUTC: new Date().toUTCString(),
            layers: mergedLayers
        };

        writeTextFile(jsonFile, stringifyJson(payload, 0));
        writeTextFile(csvFile, buildCsv(mergedLayers));

        alert(
            "Done ✅\n\n" +
            "This run exported: " + runRows.length + " layer(s)\n" +
            "Added: " + mergeInfo.added + "\n" +
            "Updated: " + mergeInfo.updated + "\n" +
            "Total rows in file: " + mergedLayers.length + "\n\n" +
            "Saved next to PSD:\n" +
            jsonFile.name + "\n" +
            csvFile.name
        );

    } catch (e) {
        alert("Error: " + e);
    } finally {
        app.preferences.rulerUnits = oldUnits;
    }

    // ---------------- Helpers ----------------

    function px(unitVal) {
        return Number(unitVal.as("px"));
    }

    function round3(n) {
        return Math.round(Number(n) * 1000) / 1000;
    }

    function safeLayerKind(lyr) {
        try { return String(lyr.kind); } catch (e) { return "Unknown"; }
    }

    function collectLayersRecursive(layerContainer, out, skipHidden) {
        for (var i = 0; i < layerContainer.length; i++) {
            var lyr = layerContainer[i];

            if (skipHidden && !lyr.visible) continue;

            out.push(lyr);

            if (lyr.typename === "LayerSet") {
                collectLayersRecursive(lyr.layers, out, skipHidden);
            }
        }
    }

    function getLayerPath(layer) {
        var names = [layer.name];
        var p = layer.parent;
        while (p && p.typename !== "Document") {
            names.unshift(p.name);
            p = p.parent;
        }
        return names.join("/");
    }

    function mergeByLayerName(existingRows, newRows, info) {
        var map = {}; // key = layerName
        var i, key;

        // Seed existing
        for (i = 0; i < existingRows.length; i++) {
            var ex = existingRows[i];
            if (!ex || !ex.layerName) continue;
            map[String(ex.layerName)] = ex;
        }

        // Update/add from current run
        for (i = 0; i < newRows.length; i++) {
            var nr = newRows[i];
            if (!nr || !nr.layerName) continue;

            key = String(nr.layerName);
            if (map.hasOwnProperty(key)) info.updated++;
            else info.added++;

            map[key] = nr;
        }

        var out = [];
        for (var k in map) {
            if (map.hasOwnProperty(k)) out.push(map[k]);
        }
        return out;
    }

    function buildCsv(rows) {
        var csv = [];
        csv.push([
            "index",
            "layerName",
            "fullPath",
            "kind",
            "visible",
            "x",
            "y",
            "w",
            "h",
            "centerX",
            "centerY",
            "offsetFromDocCenterX",
            "offsetFromDocCenterY",
            "docW",
            "docH"
        ].join(","));

        for (var i = 0; i < rows.length; i++) {
            var r = rows[i];
            csv.push([
                r.index,
                qCsv(r.layerName),
                qCsv(r.fullPath),
                qCsv(r.kind),
                r.visible ? "1" : "0",
                numCsv(r.x),
                numCsv(r.y),
                numCsv(r.w),
                numCsv(r.h),
                numCsv(r.centerX),
                numCsv(r.centerY),
                numCsv(r.offsetFromDocCenterX),
                numCsv(r.offsetFromDocCenterY),
                numCsv(r.docW),
                numCsv(r.docH)
            ].join(","));
        }

        return csv.join("\n");
    }

    function numCsv(v) {
        return (typeof v === "number") ? String(v) : "0";
    }

    function qCsv(s) {
        s = String(s);
        if (s.indexOf('"') >= 0) s = s.replace(/"/g, '""');
        if (/[",\n]/.test(s)) return '"' + s + '"';
        return s;
    }

    function writeTextFile(fileObj, txt) {
        fileObj.encoding = "UTF8";
        if (!fileObj.open("w")) throw new Error("Cannot open file for writing: " + fileObj.fsName);
        fileObj.write(txt);
        fileObj.close();
    }

    function readExistingJson(fileObj) {
        if (!fileObj.exists) return null;

        try {
            fileObj.encoding = "UTF8";
            if (!fileObj.open("r")) return null;
            var txt = fileObj.read();
            fileObj.close();

            if (!txt || !/\S/.test(txt)) return null;

            // Older ExtendScript-safe parse
            var obj = eval("(" + txt + ")");
            return obj;
        } catch (e) {
            return null;
        }
    }

    // ---------- Selected layers support (Action Manager) ----------
    // Some Photoshop versions fail here -> script falls back to "All visible" confirm.

    function getSelectedLayersSafe(docRef) {
        var result = [];
        try {
            var selectedIDs = getSelectedLayerIDs();
            if (!selectedIDs.length) return result;

            var all = [];
            collectLayersRecursive(docRef.layers, all, false);

            for (var i = 0; i < all.length; i++) {
                var layerId = getLayerIdSafe(all[i]);
                if (layerId === null) continue;

                for (var j = 0; j < selectedIDs.length; j++) {
                    if (layerId === selectedIDs[j]) {
                        result.push(all[i]);
                        break;
                    }
                }
            }
        } catch (e) {}

        return result;
    }

    function getLayerIdSafe(layer) {
        try {
            if (layer.id !== undefined) return Number(layer.id);
        } catch (e) {}

        return null;
    }

    function getSelectedLayerIDs() {
        var s2t = stringIDToTypeID;
        var ids = [];

        // Multi-select path (works on many versions)
        try {
            var ref = new ActionReference();
            ref.putProperty(s2t("property"), s2t("targetLayersIDs"));
            ref.putEnumerated(s2t("document"), s2t("ordinal"), s2t("targetEnum"));
            var desc = executeActionGet(ref);

            if (desc.hasKey(s2t("targetLayersIDs"))) {
                var list = desc.getList(s2t("targetLayersIDs"));
                for (var i = 0; i < list.count; i++) {
                    var d = list.getObjectValue(i);
                    if (d.hasKey(s2t("layerID"))) {
                        ids.push(d.getInteger(s2t("layerID")));
                    }
                }
                if (ids.length) return ids;
            }
        } catch (e1) {}

        // Single selected layer fallback
        try {
            var ref2 = new ActionReference();
            ref2.putProperty(s2t("property"), s2t("layerID"));
            ref2.putEnumerated(s2t("layer"), s2t("ordinal"), s2t("targetEnum"));
            var desc2 = executeActionGet(ref2);
            if (desc2.hasKey(s2t("layerID"))) {
                ids.push(desc2.getInteger(s2t("layerID")));
            }
        } catch (e2) {}

        return ids;
    }

    // ---------- JSON stringify (older ExtendScript-safe) ----------

    function stringifyJson(value, indentLevel) {
        var indentUnit = "  ";
        var curIndent = repeatStr(indentUnit, indentLevel);
        var nextIndent = repeatStr(indentUnit, indentLevel + 1);

        if (value === null) return "null";

        var t = typeof value;
        if (t === "number") return isFinite(value) ? String(value) : "null";
        if (t === "boolean") return value ? "true" : "false";
        if (t === "string") return quoteJsonString(value);

        if (value instanceof Array) {
            if (value.length === 0) return "[]";
            var arrParts = [];
            for (var i = 0; i < value.length; i++) {
                arrParts.push(nextIndent + stringifyJson(value[i], indentLevel + 1));
            }
            return "[\n" + arrParts.join(",\n") + "\n" + curIndent + "]";
        }

        var parts = [];
        for (var k in value) {
            if (!value.hasOwnProperty(k)) continue;
            if (typeof value[k] === "undefined") continue;
            parts.push(nextIndent + quoteJsonString(k) + ": " + stringifyJson(value[k], indentLevel + 1));
        }
        if (!parts.length) return "{}";
        return "{\n" + parts.join(",\n") + "\n" + curIndent + "}";
    }

    function quoteJsonString(s) {
        s = String(s);
        s = s.replace(/\\/g, "\\\\")
             .replace(/"/g, '\\"')
             .replace(/\r/g, "\\r")
             .replace(/\n/g, "\\n")
             .replace(/\t/g, "\\t");
        return '"' + s + '"';
    }

    function repeatStr(s, n) {
        var out = "";
        for (var i = 0; i < n; i++) out += s;
        return out;
    }

})();