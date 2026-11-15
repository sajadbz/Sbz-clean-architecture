const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { test } = require("node:test");
const { runInNewContext } = require("node:vm");

const source = readFileSync(join(__dirname, "../Sbz.WebUI/wwwroot/js/admin-owner-picker.js"), "utf8");

test("FD-03 | Filtering keeps the selected owner and allows clearing the search", () => {
    const search = {
        value: "",
        addEventListener(event, handler) {
            assert.equal(event, "input");
            this.onInput = handler;
        }
    };
    const status = { textContent: "" };
    const searchRegion = {
        hidden: true,
        querySelector(selector) {
            return selector === "input" ? search : status;
        }
    };
    const select = {
        value: "zoe-id",
        options: [
            { value: "", text: "Unassigned" },
            { value: "alice-id", text: "Alice" },
            { value: "zoe-id", text: "Zoe" }
        ],
        replaceChildren(...options) {
            this.options = options;
            this.value = options[0].value;
        }
    };
    const picker = { querySelector: selector => selector === "select" ? select : searchRegion };

    runInNewContext(source, { document: { querySelector: () => picker } });

    assert.equal(searchRegion.hidden, false);
    search.value = " ALICE ";
    search.onInput();
    assert.deepEqual(select.options.map(option => option.text), ["Unassigned", "Alice", "Zoe"]);
    assert.equal(select.value, "zoe-id");
    assert.equal(status.textContent, "1 matching account.");

    select.value = "alice-id";
    search.value = "does not exist";
    search.onInput();
    assert.deepEqual(select.options.map(option => option.text), ["Unassigned", "Alice"]);
    assert.equal(select.value, "alice-id");
    assert.equal(status.textContent, "No matching accounts.");

    search.value = "";
    search.onInput();
    assert.deepEqual(select.options.map(option => option.text), ["Unassigned", "Alice", "Zoe"]);
    assert.equal(status.textContent, "");
});
