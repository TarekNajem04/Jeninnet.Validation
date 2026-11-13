//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
import { createValidator } from "./client-validator.js";

/**
 * Parses the JSON emitted by `JeninnetStaticSsrValidation.For`.
 *
 * @param {string|object[]|null|undefined} value Raw JSON or an already parsed rule array.
 * @returns {object[]} The parsed rules; empty when the payload is missing or invalid.
 */
export function parseRules(value) {
    if(Array.isArray(value)) {
        return value;
    }

    if(typeof value !== "string" || value.trim().length === 0) {
        return [];
    }

    try {
        const parsed = JSON.parse(value);
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        return [];
    }
}

/**
 * Builds a client-rules document for one member so the shared evaluator can run it.
 *
 * @param {string} memberName The member name.
 * @param {object[]} rules The rule payload.
 * @returns {object} A client-rules document with one validator and one member.
 */
export function buildPayload(memberName, rules) {
    return {
        schemaVersion: "1.0",
        validators: [{
            name: `static-ssr:${memberName}`,
            type: null,
            members: [{ name: memberName, rules: Array.isArray(rules) ? rules : [] }]
        }]
    };
}

/**
 * Evaluates one field using the shared client-rules evaluator.
 *
 * @param {{name: string, value: unknown, rules?: string|object[], attributes?: Record<string, string>}} field The field.
 * @param {object} [options] Evaluator options (`culture`, `messages`).
 * @returns {{valid: boolean, errors: object[], needsServer: boolean}} The field result.
 */
export function validateField(field, options = {}) {
    const memberName = field?.name ?? field?.attributes?.["data-jeninnet-member"];
    if(typeof memberName !== "string" || memberName.length === 0) {
        return { valid: true, errors: [], needsServer: false };
    }

    const rules = parseRules(field.rules ?? field.attributes?.["data-jeninnet-rules"]);
    if(rules.length === 0) {
        return { valid: true, errors: [], needsServer: false };
    }

    const validator = createValidator(buildPayload(memberName, rules), options);
    const result = validator.validate({ [memberName]: field.value });
    const errors = (result.errors ?? []).filter(error => error.member === memberName || !error.member);

    return {
        valid: errors.length === 0,
        errors,
        needsServer: result.needsServer === true
    };
}

/**
 * Evaluates a set of fields. Server-only rules do not make the form invalid.
 *
 * @param {Array<{name: string, value: unknown, rules?: string|object[]}>} fields The fields.
 * @param {object} [options] Evaluator options.
 * @returns {{valid: boolean, errors: object[], needsServer: boolean}} The aggregate result.
 */
export function validateForm(fields, options = {}) {
    const results = (fields ?? []).map(field => validateField(field, options));

    return {
        valid: results.every(result => result.valid),
        errors: results.flatMap(result => result.errors),
        needsServer: results.some(result => result.needsServer)
    };
}

/**
 * Enhances a static SSR form: validates fields before submission and renders messages next to
 * them. The server remains authoritative; server-only rules never block submission.
 *
 * @param {HTMLFormElement} form The form to enhance.
 * @param {object} [options] Evaluator options plus an optional `document` for tests.
 * @returns {void}
 */
export function attach(form, options = {}) {
    if(form === null || form === undefined || form.dataset?.jeninnetBound === "true") {
        return;
    }

    const document = options.document ?? globalThis.document;
    const fields = () => Array.from(form.querySelectorAll("[data-jeninnet-rules]")).map(element => ({
        element,
        name: element.getAttribute("data-jeninnet-member") ?? element.name,
        value: element.value,
        rules: element.getAttribute("data-jeninnet-rules")
    }));

    const render = (field, result) => {
        if(!field.element || typeof field.element.setAttribute !== "function") {
            return;
        }

        field.element.setAttribute("aria-invalid", result.valid ? "false" : "true");

        let message = findMessage(form, field.name);
        if(!result.valid && message === null) {
            message = document.createElement("div");
            message.className = "jeninnet-validation-message";
            message.setAttribute("data-jeninnet-message-for", field.name);
            field.element.insertAdjacentElement?.("afterend", message);
        }

        if(message !== null) {
            message.textContent = result.valid ? "" : result.errors.map(error => error.message).join(" ");
        }
    };

    const validate = () => {
        let valid = true;

        for(const field of fields()) {
            const result = validateField(field, options);
            render(field, result);
            valid = valid && result.valid;
        }

        return valid;
    };

    form.dataset.jeninnetBound = "true";
    form.noValidate = true;

    form.addEventListener("submit", event => {
        if(!validate()) {
            event.preventDefault();
        }
    });

    form.addEventListener("input", event => {
        const target = event.target;
        if(typeof target?.getAttribute !== "function" ||
           target.getAttribute("data-jeninnet-rules") === null) {
            return;
        }

        const name = target.getAttribute("data-jeninnet-member") ?? target.name;
        render({ element: target, name, value: target.value, rules: target.getAttribute("data-jeninnet-rules") },
            validateField({ name, value: target.value, rules: target.getAttribute("data-jeninnet-rules") }, options));
    });
}

/**
 * Attaches the module to every marked form in the document.
 *
 * @param {Document|HTMLElement} [root] The root to search; defaults to `document`.
 * @param {object} [options] Evaluator options.
 * @returns {void}
 */
export function attachAll(root = globalThis.document, options = {}) {
    for(const form of root.querySelectorAll("form[data-jeninnet-validation]")) {
        attach(form, options);
    }
}

function findMessage(form, memberName) {
    for(const message of form.querySelectorAll("[data-jeninnet-message-for]")) {
        if(message.getAttribute("data-jeninnet-message-for") === memberName) {
            return message;
        }
    }

    return null;
}

if(typeof globalThis.document !== "undefined") {
    if(globalThis.document.readyState === "loading") {
        globalThis.document.addEventListener("DOMContentLoaded", () => attachAll());
    } else {
        attachAll();
    }
}
