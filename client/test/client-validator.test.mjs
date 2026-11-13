//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";
import { createValidator, needsServer, validate } from "../src/client-validator.js";
import { defaultMessages } from "../src/messages.js";

const payload = {
    schemaVersion: "1.0",
    validators: [
        {
            name: "Sample.CustomerValidator",
            type: "Sample.Customer",
            members: [
                {
                    name: "Email",
                    rules: [
                        { rule: "NotEmpty", messageKey: "NotEmpty", severity: "Error", hasCondition: false, params: {} },
                        { rule: "Email", messageKey: "Email", severity: "Error", hasCondition: false, params: {} }
                    ]
                },
                {
                    name: "Name",
                    rules: [
                        {
                            rule: "Length",
                            messageKey: "Length",
                            severity: "Error",
                            hasCondition: false,
                            params: { Min: 2, Max: 10 }
                        }
                    ]
                }
            ]
        }
    ]
};

test("validates supported rules and returns stable member errors", () => {
    const result = createValidator(payload).validate({ Email: "", Name: "a" });

    assert.equal(result.isValid, false);
    assert.equal(result.valid, false);
    assert.equal(result.needsServer, false);
    assert.equal(result.errors.length, 2);
    assert.equal(result.errors[0].member, "Email");
    assert.equal(result.errors[0].messageKey, "NotEmpty");
    assert.equal(result.errors[1].member, "Name");
    assert.match(result.errors[1].message, /between 2 and 10/);
});

test("accepts a valid model", () => {
    const result = validate(payload, { Email: "person@example.com", Name: "Tarek" });

    assert.equal(result.isValid, true);
    assert.deepEqual(result.errors, []);
});

test("resolves English and Arabic message packs", () => {
    const english = createValidator(payload, { culture: "en" }).validate({ Email: "", Name: "ok" });
    const arabic = createValidator(payload, { culture: "ar" }).validate({ Email: "", Name: "ok" });

    assert.match(english.errors[0].message, /required/);
    assert.match(arabic.errors[0].message, /مطلوب/);
    assert.equal(defaultMessages.ar.NotEmpty.includes("{PropertyName}"), true);
});

test("marks unsupported and conditional rules as server-only", () => {
    const result = createValidator({
        schemaVersion: "1.0",
        validators: [{
            name: "Sample.ComplexValidator",
            type: "Sample.Complex",
            members: [{
                name: "Value",
                rules: [
                    { rule: "Must", messageKey: "Must", severity: "Error", hasCondition: false, serverOnly: true, params: {} },
                    { rule: "NotEmpty", messageKey: "NotEmpty", severity: "Error", hasCondition: true, params: {} }
                ]
            }]
        }]
    }).validate({ Value: "" });

    assert.equal(result.needsServer, true);
    assert.equal(needsServer(result), true);
    assert.equal(result.errors.length, 0);
});

test("validates email and trims trailing zeros when requested", () => {
    const validator = createValidator({
        schemaVersion: "1.0",
        validators: [{
            name: "Sample.FormatValidator",
            type: "Sample.Format",
            members: [
                { name: "Email", rules: [{ rule: "Email", messageKey: "Email", severity: "Error", hasCondition: false, params: {} }] },
                {
                    name: "Amount",
                    rules: [{
                        rule: "ScalePrecision",
                        messageKey: "ScalePrecision",
                        severity: "Error",
                        hasCondition: false,
                        params: { Precision: 5, Scale: 2, IgnoreTrailingZeros: true }
                    }]
                }
            ]
        }]
    });

    assert.equal(validator.validate({ Email: "person@example.com", Amount: "12.3400" }).isValid, true);
    assert.equal(validator.validate({ Email: "person@example", Amount: "12.3400" }).isValid, false);
});

test("evaluates collection, membership, file extension, and regex rules", () => {
    const result = createValidator({
        schemaVersion: "1.0",
        validators: [{
            name: "Sample.RulesValidator",
            type: "Sample.Rules",
            members: [
                { name: "Count", rules: [{ rule: "MinCount", messageKey: "MinCount", severity: "Error", hasCondition: false, params: { Min: 2 } }] },
                { name: "Mode", rules: [{ rule: "In", messageKey: "In", severity: "Error", hasCondition: false, params: { Values: "a, b" } }] },
                { name: "File", rules: [{ rule: "FileExtensions", messageKey: "FileExtensions", severity: "Error", hasCondition: false, params: { Extensions: ".pdf, .txt" } }] },
                { name: "Code", rules: [{ rule: "Regex", messageKey: "Regex", severity: "Error", hasCondition: false, params: { Pattern: "^[A-Z]{3}$" } }] }
            ]
        }]
    }).validate({ Count: [1], Mode: "c", File: "report.doc", Code: "abc" });

    assert.equal(result.errors.length, 4);
});

test("is dependency-free and does not use eval or Function", async () => {
    const source = await readFile(new URL("../src/client-validator.js", import.meta.url), "utf8");

    assert.equal(/\beval\s*\(/.test(source), false);
    assert.equal(/\bnew\s+Function\s*\(/.test(source), false);
});
