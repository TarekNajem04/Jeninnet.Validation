//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
import { defaultMessages } from "./messages.js";

const supportedRules = new Set([
    "NotNull",
    "NotEmpty",
    "StringNotEmpty",
    "Length",
    "ExactLength",
    "Regex",
    "Email",
    "Url",
    "IPAddress",
    "Phone",
    "CreditCard",
    "Base64String",
    "FileExtensions",
    "Contains",
    "StartsWith",
    "EndsWith",
    "Count",
    "MinCount",
    "MaxCount",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "InclusiveBetween",
    "ExclusiveBetween",
    "Range",
    "In",
    "NotIn",
    "ScalePrecision",
    "After",
    "Before"
]);

/**
 * Creates a browser-side validator from a client-rules document.
 *
 * @param {object} payload The `ClientRulesDocument` payload.
 * @param {object} [options] Runtime options.
 * @param {string} [options.validatorName] Select a validator by name; defaults to the first.
 * @param {string} [options.culture] Message culture; defaults to `en`.
 * @param {object} [options.messages] Message packs keyed by culture.
 * @returns {{name: string, type: string|null, validate: (model: unknown) => object}}
 */
export function createValidator(payload, options = {}) {
    if(payload === null || typeof payload !== "object" || !Array.isArray(payload.validators) || payload.validators.length === 0) {
        throw new TypeError("A client-rules document with at least one validator is required.");
    }

    const validator = options.validatorName === undefined
        ? payload.validators[0]
        : payload.validators.find(candidate => candidate.name === options.validatorName);

    if(validator === undefined) {
        throw new Error(`Validator '${options.validatorName}' was not found in the client-rules document.`);
    }

    const culture = options.culture ?? "en";
    const messages = options.messages ?? defaultMessages;

    function validate(model) {
        const context = {
            culture,
            messages,
            errors: [],
            needsServer: false
        };

        for(const member of validator.members ?? []) {
            const value = getValue(model, member.name);

            for(const rule of member.rules ?? []) {
                evaluateRule(rule, member.name, value, context);
            }
        }

        return {
            validator: validator.name,
            isValid: context.errors.length === 0,
            valid: context.errors.length === 0,
            errors: context.errors,
            needsServer: context.needsServer
        };
    }

    return {
        name: validator.name,
        type: validator.type ?? null,
        validate
    };
}

/**
 * Convenience wrapper around {@link createValidator}.
 */
export function validate(payload, model, options = {}) {
    return createValidator(payload, options).validate(model);
}

/**
 * Returns whether a result contains rules that need the server.
 */
export function needsServer(result) {
    return Boolean(result?.needsServer);
}

function evaluateRule(rule, memberName, value, context) {
    if(rule.serverOnly === true || rule.hasCondition === true || !supportedRules.has(rule.rule)) {
        context.needsServer = true;
        return;
    }

    if(!passes(rule, value)) {
        addError(rule, memberName, context);
    }
}

function passes(rule, value) {
    switch(rule.rule) {
        case "NotNull":
            return value !== null && value !== undefined;

        case "NotEmpty":
        case "StringNotEmpty":
            return !isEmpty(value);

        case "Length": {
            if(isNil(value)) {
                return true;
            }

            const length = lengthOf(value);
            return length === null || inRange(length, rule.params?.Min, rule.params?.Max);
        }

        case "ExactLength": {
            if(isNil(value)) {
                return true;
            }

            const length = lengthOf(value);
            return length === null || length === toNumber(rule.params?.Length);
        }

        case "Regex":
            return isNilOrEmptyString(value) || matchesPattern(value, rule.params?.Pattern);

        case "Email":
            return isNilOrEmptyString(value) || isEmail(String(value));

        case "Url":
            return isNilOrEmptyString(value) || isUrl(String(value));

        case "IPAddress":
            return isNilOrEmptyString(value) || isIpAddress(String(value));

        case "Phone":
            return isNilOrEmptyString(value) || /^\+?\d[\d\s().-]{5,}$/.test(String(value));

        case "CreditCard":
            return isNilOrEmptyString(value) || isCreditCard(String(value));

        case "Base64String":
            return isNilOrEmptyString(value) || isBase64(String(value));

        case "FileExtensions":
            return isNil(value) || hasAllowedExtension(String(value), rule.params?.Extensions);

        case "Contains":
            return isNilOrEmptyString(value) || String(value).includes(String(rule.params?.Value ?? ""));

        case "StartsWith":
            return isNilOrEmptyString(value) || String(value).startsWith(String(rule.params?.Value ?? ""));

        case "EndsWith":
            return isNilOrEmptyString(value) || String(value).endsWith(String(rule.params?.Value ?? ""));

        case "Count":
            return isNil(value) || inRange(countOf(value), rule.params?.Min, rule.params?.Max);

        case "MinCount":
            return isNil(value) || countOf(value) >= toNumber(rule.params?.Min);

        case "MaxCount":
            return isNil(value) || countOf(value) <= toNumber(rule.params?.Max);

        case "GreaterThan":
            return isNil(value) || toNumber(value) > toNumber(rule.params?.Threshold);

        case "GreaterThanOrEqual":
            return isNil(value) || toNumber(value) >= toNumber(rule.params?.Threshold);

        case "LessThan":
            return isNil(value) || toNumber(value) < toNumber(rule.params?.Threshold);

        case "LessThanOrEqual":
            return isNil(value) || toNumber(value) <= toNumber(rule.params?.Threshold);

        case "InclusiveBetween":
        case "Range":
            return isNil(value) || inRange(toNumber(value), rule.params?.Min, rule.params?.Max);

        case "ExclusiveBetween":
            return isNil(value) ||
                (toNumber(value) > toNumber(rule.params?.Min) && toNumber(value) < toNumber(rule.params?.Max));

        case "In":
            return isNil(value) || splitValues(rule.params?.Values).includes(String(value));

        case "NotIn":
            return isNil(value) || !splitValues(rule.params?.Values).includes(String(value));

        case "ScalePrecision":
            return isNil(value) || hasScalePrecision(value, rule.params);

        case "After":
        case "Before":
            return isNil(value) || compareDate(value, rule);

        default:
            return true;
    }
}

function addError(rule, memberName, context) {
    const messageKey = rule.messageKey ?? rule.rule;
    const params = {
        ...rule.params,
        PropertyName: rule.displayName ?? memberName
    };

    context.errors.push({
        member: memberName,
        rule: rule.rule,
        messageKey,
        message: rule.message ?? resolveMessage(context.messages, context.culture, messageKey, params),
        severity: rule.severity ?? "Error",
        params,
        ...(rule.errorCode === undefined ? {} : { errorCode: rule.errorCode })
    });
}

function resolveMessage(messages, culture, messageKey, params) {
    const pack = messages?.[culture] ?? messages?.en ?? {};
    const template = pack[messageKey] ?? messageKey;

    return formatMessage(template, params);
}

function formatMessage(template, params) {
    let result = "";
    let index = 0;

    while(index < template.length) {
        const start = template.indexOf("{", index);

        if(start < 0) {
            result += template.slice(index);
            break;
        }

        const end = template.indexOf("}", start + 1);

        if(end < 0) {
            result += template.slice(index);
            break;
        }

        if(end === start + 1) {
            result += template.slice(index, end + 1);
            index = end + 1;
            continue;
        }

        result += template.slice(index, start);
        result += String(params[template.slice(start + 1, end)] ?? "");
        index = end + 1;
    }

    return result;
}

function getValue(model, memberName) {
    return model === null || model === undefined ? undefined : model[memberName];
}

function isNil(value) {
    return value === null || value === undefined;
}

function isNilOrEmptyString(value) {
    return isNil(value) || value === "";
}

function isEmail(value) {
    if(/\s/.test(value)) {
        return false;
    }

    const at = value.indexOf("@");

    if(at <= 0 || value.includes("@", at + 1)) {
        return false;
    }

    const domain = value.slice(at + 1);
    const lastDot = domain.lastIndexOf(".");

    return lastDot > 0 && lastDot < domain.length - 1;
}

function isEmpty(value) {
    if(isNil(value)) {
        return true;
    }

    if(typeof value === "string") {
        return value.trim().length === 0;
    }

    if(Array.isArray(value)) {
        return value.length === 0;
    }

    if(value instanceof Map || value instanceof Set) {
        return value.size === 0;
    }

    return false;
}

function lengthOf(value) {
    return typeof value === "string" || Array.isArray(value) ? value.length : null;
}

function countOf(value) {
    return typeof value === "string" || Array.isArray(value) ? value.length : 0;
}

function inRange(value, min, max) {
    return value >= toNumber(min) && value <= toNumber(max);
}

function toNumber(value) {
    const number = Number(value);
    return Number.isFinite(number) ? number : 0;
}

function matchesPattern(value, pattern) {
    if(isNil(value) || typeof pattern !== "string") {
        return true;
    }

    try {
        return new RegExp(pattern).test(String(value));
    } catch {
        return true;
    }
}

function isUrl(value) {
    try {
        new URL(value);
        return true;
    } catch {
        return false;
    }
}

function isIpAddress(value) {
    return /^(\d{1,3}\.){3}\d{1,3}$/.test(value) ||
        /^([0-9a-f]{1,4}:){2,7}[0-9a-f]{1,4}$/i.test(value) ||
        value === "::1" ||
        value === "::";
}

function isCreditCard(value) {
    const digits = value.replaceAll(/\D/g, "");

    if(digits.length < 12) {
        return false;
    }

    let sum = 0;
    let double = false;

    for(let i = digits.length - 1; i >= 0; i--) {
        let digit = Number(digits[i]);

        if(double) {
            digit *= 2;
            if(digit > 9) {
                digit -= 9;
            }
        }

        sum += digit;
        double = !double;
    }

    return sum % 10 === 0;
}

function isBase64(value) {
    return /^[A-Za-z0-9+/]*={0,2}$/.test(value) && value.length % 4 === 0;
}

function hasAllowedExtension(value, extensions) {
    const dot = value.lastIndexOf(".");
    const extension = dot >= 0 ? value.slice(dot).toLowerCase() : "";
    const allowed = String(extensions ?? "")
        .split(",")
        .map(staticExtension => staticExtension.trim().toLowerCase())
        .filter(Boolean)
        .map(item => item.startsWith(".") ? item : `.${item}`);

    return allowed.includes(extension);
}

function splitValues(values) {
    return String(values ?? "")
        .split(",")
        .map(value => value.trim())
        .filter(Boolean);
}

function hasScalePrecision(value, params) {
    const precision = toNumber(params?.Precision);
    const scale = toNumber(params?.Scale);
    const text = String(value);
    const [integer, fraction = ""] = text.split(".");

    const normalizedFraction = params?.IgnoreTrailingZeros === true
        ? trimTrailingZeros(fraction)
        : fraction;

    return integer.replace(/\D/g, "").length + normalizedFraction.length <= precision &&
        normalizedFraction.length <= scale;
}

function trimTrailingZeros(value) {
    let end = value.length;

    while(end > 0 && value[end - 1] === "0") {
        end--;
    }

    return end === value.length ? value : value.slice(0, end);
}

function compareDate(value, rule) {
    const date = new Date(value);
    const threshold = new Date(rule.params?.Threshold);

    if(Number.isNaN(date.getTime()) || Number.isNaN(threshold.getTime())) {
        return true;
    }

    return rule.rule === "After" ? date > threshold : date < threshold;
}
