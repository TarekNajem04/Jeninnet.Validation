//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

export type ClientRuleSeverity = "Error" | "Warning" | "Info";

export interface ClientRule {
    readonly rule: string;
    readonly messageKey: string | null;
    readonly message?: string;
    readonly severity: ClientRuleSeverity;
    readonly hasCondition: boolean;
    readonly serverOnly?: boolean;
    readonly errorCode?: string;
    readonly displayName?: string;
    readonly params: Readonly<Record<string, string | number | boolean | null>>;
}

export interface ClientMember {
    readonly name: string;
    readonly rules: readonly ClientRule[];
}

export interface ClientValidator {
    readonly name: string;
    readonly type?: string | null;
    readonly members: readonly ClientMember[];
}

export interface ClientRulesDocument {
    readonly schemaVersion: string;
    readonly validators: readonly ClientValidator[];
}

export interface ClientValidationError {
    readonly member: string;
    readonly rule: string;
    readonly messageKey: string;
    readonly message: string;
    readonly severity: ClientRuleSeverity;
    readonly params: Readonly<Record<string, unknown>>;
    readonly errorCode?: string;
}

export interface ClientValidationResult {
    readonly validator: string;
    readonly isValid: boolean;
    readonly valid: boolean;
    readonly errors: readonly ClientValidationError[];
    readonly needsServer: boolean;
}

export interface ClientValidatorOptions {
    readonly validatorName?: string;
    readonly culture?: string;
    readonly messages?: Readonly<Record<string, Readonly<Record<string, string>>>>;
}

export interface ClientRuntimeValidator {
    readonly name: string;
    readonly type: string | null;
    validate(model: unknown): ClientValidationResult;
}

export function createValidator(
    payload: ClientRulesDocument,
    options?: ClientValidatorOptions
): ClientRuntimeValidator;

export function validate(
    payload: ClientRulesDocument,
    model: unknown,
    options?: ClientValidatorOptions
): ClientValidationResult;

export function needsServer(result: ClientValidationResult | undefined | null): boolean;
