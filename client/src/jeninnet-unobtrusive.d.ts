//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

/** A client-rule payload emitted by the generated client-rules catalog. */
export interface ClientRule {
    readonly rule: string;
    readonly messageKey?: string;
    readonly message?: string;
    readonly severity?: string;
    readonly hasCondition?: boolean;
    readonly serverOnly?: boolean;
    readonly errorCode?: string;
    readonly displayName?: string;
    readonly params?: Readonly<Record<string, string | number | boolean | null>>;
}

/** A member rule payload stored in `data-jeninnet-rules`. */
export interface StaticSsrField {
    readonly name: string;
    readonly value: unknown;
    readonly rules?: string | readonly ClientRule[];
    readonly attributes?: Readonly<Record<string, string>>;
}

/** One validation error returned by the shared client evaluator. */
export interface ClientValidationError {
    readonly member: string;
    readonly rule: string;
    readonly messageKey: string;
    readonly message: string;
    readonly severity: string;
    readonly params: Readonly<Record<string, unknown>>;
    readonly errorCode?: string;
}

/** The result of evaluating one field or form. */
export interface FieldValidationResult {
    readonly valid: boolean;
    readonly errors: readonly ClientValidationError[];
    readonly needsServer: boolean;
}

/** Parses a `data-jeninnet-rules` payload. */
export function parseRules(value: string | readonly ClientRule[] | null | undefined): ClientRule[];

/** Builds a one-member client-rules document for the shared evaluator. */
export function buildPayload(memberName: string, rules: readonly ClientRule[]): object;

/** Evaluates one field using the shared client-rules evaluator. */
export function validateField(field: StaticSsrField, options?: object): FieldValidationResult;

/** Evaluates several fields; server-only rules do not make the form invalid. */
export function validateForm(fields: readonly StaticSsrField[], options?: object): FieldValidationResult;

/** Enhances a static SSR form and renders messages next to fields. */
export function attach(form: HTMLFormElement, options?: object): void;

/** Attaches the module to every `form[data-jeninnet-validation]` under the root. */
export function attachAll(root?: Document | HTMLElement, options?: object): void;
