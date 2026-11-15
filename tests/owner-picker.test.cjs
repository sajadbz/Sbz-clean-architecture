const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { test } = require("node:test");
const { runInNewContext } = require("node:vm");

const source = readFileSync(join(__dirname, "../Sbz.WebUI/wwwroot/js/admin-owner-picker.js"), "utf8");

class Element {
    constructor() {
        this.attributes = new Map();
        this.handlers = new Map();
        this.children = [];
        this.hidden = false;
    }

    addEventListener(name, handler) { this.handlers.set(name, handler); }
    getAttribute(name) { return this.attributes.get(name) ?? null; }
    setAttribute(name, value) { this.attributes.set(name, value); }
    removeAttribute(name) { this.attributes.delete(name); }
    replaceChildren(...children) { this.children = children; }
    scrollIntoView() {}
    select() {}

    emit(name, key) {
        const event = { key, preventDefault() { this.defaultPrevented = true; } };
        this.handlers.get(name)?.(event);
        return event;
    }
}

function createPicker() {
    const input = Object.assign(new Element(), { id: "owner-combobox", value: "" });
    const list = Object.assign(new Element(), { id: "owner-options" });
    const popup = Object.assign(new Element(), { hidden: true });
    const enhanced = Object.assign(new Element(), { hidden: true });
    const status = new Element();
    const label = new Element();
    const select = Object.assign(new Element(), {
        value: "zoe-id",
        options: [
            { value: "", text: "Unassigned" },
            { value: "alice-id", text: "Alice" },
            { value: "alex-id", text: "Alex" },
            { value: "zoe-id", text: "Zoe" }
        ]
    });
    const selectors = new Map([
        ["select", select], ["label", label], ["[data-owner-combobox]", enhanced],
        ["[role=combobox]", input], ["[data-owner-popup]", popup],
        ["[role=listbox]", list], ["[data-owner-status]", status]
    ]);
    const picker = { querySelector: selector => selectors.get(selector) };

    runInNewContext(source, { document: { querySelector: () => picker, createElement: () => new Element() } });
    return { input, list, popup, enhanced, select, status, label };
}

test("FD-03 | One Owner field opens matching options and accepts a keyboard choice", () => {
    const { input, list, popup, enhanced, select, label } = createPicker();
    assert.equal(enhanced.hidden, false);
    assert.equal(select.hidden, true);
    assert.equal(label.htmlFor, input.id);
    assert.equal(input.value, "Zoe");

    input.emit("focus");
    assert.equal(popup.hidden, false);
    input.value = " AL ";
    input.emit("input");
    assert.deepEqual(Array.from(list.children, option => option.textContent), ["Alice", "Alex"]);
    assert.equal(select.value, "zoe-id");

    assert.equal(input.emit("keydown", "ArrowDown").defaultPrevented, true);
    assert.equal(input.getAttribute("aria-activedescendant"), list.children[0].id);
    assert.equal(input.emit("keydown", "Enter").defaultPrevented, true);
    assert.equal(select.value, "alice-id");
    assert.equal(input.value, "Alice");
    assert.equal(popup.hidden, true);
    assert.equal(input.getAttribute("aria-activedescendant"), null);
});

test("FD-03 | Canceling search keeps the owner, and selecting Unassigned clears it", () => {
    const { input, list, popup, select, status } = createPicker();
    input.emit("click");
    input.value = "unknown";
    input.emit("input");
    assert.equal(list.children.length, 0);
    assert.equal(status.textContent, "No matching accounts.");
    assert.equal(input.emit("keydown", "Escape").defaultPrevented, true);
    assert.equal(input.value, "Zoe");
    assert.equal(select.value, "zoe-id");
    assert.equal(popup.hidden, true);

    input.value = "alice";
    input.emit("input");
    input.emit("blur");
    assert.equal(input.value, "Zoe");
    assert.equal(select.value, "zoe-id");

    input.emit("click");
    list.children.find(option => option.textContent === "Unassigned").emit("click");
    assert.equal(select.value, "");
    assert.equal(input.value, "");
    assert.equal(popup.hidden, true);
});
