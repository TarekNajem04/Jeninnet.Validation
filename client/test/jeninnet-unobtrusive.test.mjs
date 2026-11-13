//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
import assert from "node:assert/strict";
import { test } from "node:test";
import {
    attach,
    buildPayload,
    parseRules,
    validateField,
    validateForm
} from "../src/jeninnet-unobtrusive.js";

const requiredRules = JSON.stringify([
    { rule: "StringNotEmpty", messageKey: "NotEmpty", severity: "Error", hasCondition: false, params: {} }
]);

const mixedRules = JSON.stringify([
    { rule: "StringNotEmpty", messageKey: "NotEmpty", severity: "Error", hasCondition: false, params: {} },
    { rule: "Length", messageKey: "Length", severity: "Error", hasCondition: false, params: { Min: 2, Max: 10 } }
]);

const serverOnlyRules = JSON.stringify([
    { rule: "FutureDate", messageKey: "FutureDate", severity: "Error", hasCondition: false, serverOnly: true, params: {} }
]);

test("parseRules accepts JSON and arrays and rejects malformed payloads", () => {
    assert.deepEqual(parseRules(requiredRules), JSON.parse(requiredRules));
    assert.deepEqual(parseRules([{ rule: "Email" }]), [{ rule: "Email" }]);
    assert.deepEqual(parseRules("not-json"), []);
    assert.deepEqual(parseRules("{}"), []);
    assert.deepEqual(parseRules(""), []);
});

test("buildPayload wraps one member for the shared evaluator", () => {
    const payload = buildPayload("Name", JSON.parse(requiredRules));

    assert.equal(payload.schemaVersion, "1.0");
    assert.equal(payload.validators[0].members[0].name, "Name");
    assert.equal(payload.validators[0].members[0].rules[0].rule, "StringNotEmpty");
});

test("validateField blocks an empty required value and reports its message", () => {
    const result = validateField({ name: "Name", value: "", rules: requiredRules });

    assert.equal(result.valid, false);
    assert.equal(result.needsServer, false);
    assert.equal(result.errors[0].member, "Name");
    assert.match(result.errors[0].message, /required/);
});

test("validateField accepts a value that satisfies every client rule", () => {
    const result = validateField({ name: "Name", value: "Valid Name", rules: mixedRules });

    assert.equal(result.valid, true);
    assert.equal(result.errors.length, 0);
});

test("validateField reports a length failure with the localized message", () => {
    const result = validateField({ name: "Name", value: "x", rules: mixedRules });

    assert.equal(result.valid, false);
    assert.match(result.errors[0].message, /between 2 and 10/);
});

test("server-only rules do not block the browser and request the server", () => {
    const result = validateField({ name: "StartsAt", value: "2020-01-01", rules: serverOnlyRules });

    assert.equal(result.valid, true);
    assert.equal(result.needsServer, true);
});

test("validateForm aggregates field results", () => {
    const invalid = validateForm([
        { name: "Name", value: "", rules: requiredRules },
        { name: "Email", value: "bad", rules: JSON.stringify([
            { rule: "Email", messageKey: "Email", severity: "Error", hasCondition: false, params: {} }
        ]) }
    ]);

    assert.equal(invalid.valid, false);
    assert.equal(invalid.errors.length, 2);

    const valid = validateForm([
        { name: "Name", value: "Valid Name", rules: mixedRules },
        { name: "StartsAt", value: "2020-01-01", rules: serverOnlyRules }
    ]);

    assert.equal(valid.valid, true);
    assert.equal(valid.needsServer, true);
});

test("attach prevents an invalid submit and renders the message next to the field", () => {
    const field = new FakeElement("input", {
        name: "Name",
        "data-jeninnet-member": "Name",
        "data-jeninnet-rules": requiredRules
    });
    field.value = "";

    const form = new FakeForm([field]);
    field.insertAdjacentElement = (_, element) => form.messages.push(element);

    attach(form, { document: new FakeDocument() });

    let prevented = false;
    form.dispatch("submit", { preventDefault: () => { prevented = true; } });

    assert.equal(prevented, true);
    assert.equal(form.messages.length, 1);
    assert.equal(form.messages[0].dataset.jeninnetMessageFor, "Name");
    assert.match(form.messages[0].textContent, /required/);
    assert.equal(field.getAttribute("aria-invalid"), "true");

    field.value = "Valid Name";
    form.dispatch("input", { target: field });

    assert.equal(field.getAttribute("aria-invalid"), "false");
    assert.equal(form.messages[0].textContent, "");
});

test("attach does not block when only server-only rules are present", () => {
    const field = new FakeElement("input", {
        name: "StartsAt",
        "data-jeninnet-member": "StartsAt",
        "data-jeninnet-rules": serverOnlyRules
    });
    field.value = "2020-01-01";

    const form = new FakeForm([field]);
    attach(form, { document: new FakeDocument() });

    let prevented = false;
    form.dispatch("submit", { preventDefault: () => { prevented = true; } });

    assert.equal(prevented, false);
    assert.equal(form.messages.length, 0);
});

class FakeDocument {
    createElement(tagName) {
        return new FakeElement(tagName);
    }
}

class FakeElement {
    constructor(tagName, attributes = {}) {
        this.tagName = tagName;
        this.attributes = { ...attributes };
        this.dataset = {};
        for(const [key, value] of Object.entries(attributes)) {
            if(key.startsWith("data-")) {
                const datasetKey = key.slice(5).replace(/-([a-z])/g, (_, character) => character.toUpperCase());
                this.dataset[datasetKey] = String(value);
            }
        }
        this.listeners = {};
        this.name = attributes.name ?? "";
        this.value = "";
        this.textContent = "";
        this.noValidate = false;
    }

    getAttribute(name) {
        return Object.hasOwn(this.attributes, name) ? this.attributes[name] : null;
    }

    setAttribute(name, value) {
        this.attributes[name] = String(value);
    }

    addEventListener(type, handler) {
        this.listeners[type] ??= [];
        this.listeners[type].push(handler);
    }

    dispatch(type, event) {
        for(const handler of this.listeners[type] ?? []) {
            handler(event);
        }
    }

    insertAdjacentElement(_position, _element) {
    }
}

class FakeForm extends FakeElement {
    constructor(fields) {
        super("form", { "data-jeninnet-validation": "true" });
        this.fields = fields;
        this.messages = [];
    }

    querySelectorAll(selector) {
        if(selector === "[data-jeninnet-rules]") {
            return this.fields;
        }

        if(selector === "[data-jeninnet-message-for]") {
            return this.messages;
        }

        return [];
    }
}
