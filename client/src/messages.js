//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

/**
 * Built-in client message packs. Templates use `{Placeholder}` tokens resolved from rule
 * parameters and `PropertyName`.
 *
 * The server remains authoritative; these messages are early-feedback text only.
 */
const messageKeys = [
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
];

const englishMessages = [
    "{PropertyName} is required.",
    "{PropertyName} is required.",
    "{PropertyName} is required.",
    "{PropertyName} must be between {Min} and {Max} characters.",
    "{PropertyName} must be exactly {Length} characters.",
    "{PropertyName} has an invalid format.",
    "{PropertyName} must be a valid email address.",
    "{PropertyName} must be a valid URL.",
    "{PropertyName} must be a valid IP address.",
    "{PropertyName} must be a valid phone number.",
    "{PropertyName} must be a valid card number.",
    "{PropertyName} must be valid base64.",
    "{PropertyName} has an unsupported file extension.",
    "{PropertyName} must contain {Value}.",
    "{PropertyName} must start with {Value}.",
    "{PropertyName} must end with {Value}.",
    "{PropertyName} must contain between {Min} and {Max} items.",
    "{PropertyName} must contain at least {Min} items.",
    "{PropertyName} must contain at most {Max} items.",
    "{PropertyName} must be greater than {Threshold}.",
    "{PropertyName} must be at least {Threshold}.",
    "{PropertyName} must be less than {Threshold}.",
    "{PropertyName} must be at most {Threshold}.",
    "{PropertyName} must be between {Min} and {Max}.",
    "{PropertyName} must be strictly between {Min} and {Max}.",
    "{PropertyName} must be between {Min} and {Max}.",
    "{PropertyName} must be one of: {Values}.",
    "{PropertyName} must not be one of: {Values}.",
    "{PropertyName} must have at most {Precision} digits with scale {Scale}.",
    "{PropertyName} must be after {Threshold}.",
    "{PropertyName} must be before {Threshold}."
];

const arabicMessages = [
    "{PropertyName} مطلوب.",
    "{PropertyName} مطلوب.",
    "{PropertyName} مطلوب.",
    "يجب أن يكون طول {PropertyName} بين {Min} و{Max} حرفًا.",
    "يجب أن يكون طول {PropertyName} مساويًا لـ {Length} حرفًا.",
    "تنسيق {PropertyName} غير صالح.",
    "يجب أن يكون {PropertyName} بريدًا إلكترونيًا صالحًا.",
    "يجب أن يكون {PropertyName} رابطًا صالحًا.",
    "يجب أن يكون {PropertyName} عنوان IP صالحًا.",
    "يجب أن يكون {PropertyName} رقم هاتف صالحًا.",
    "يجب أن يكون {PropertyName} رقم بطاقة صالحًا.",
    "يجب أن يكون {PropertyName} بترميز base64 صالح.",
    "امتداد ملف {PropertyName} غير مدعوم.",
    "يجب أن يحتوي {PropertyName} على {Value}.",
    "يجب أن يبدأ {PropertyName} بـ {Value}.",
    "يجب أن ينتهي {PropertyName} بـ {Value}.",
    "يجب أن يحتوي {PropertyName} على عدد عناصر بين {Min} و{Max}.",
    "يجب أن يحتوي {PropertyName} على {Min} عنصرًا على الأقل.",
    "يجب أن يحتوي {PropertyName} على {Max} عنصر كحد أقصى.",
    "يجب أن يكون {PropertyName} أكبر من {Threshold}.",
    "يجب أن يكون {PropertyName} أكبر من أو يساوي {Threshold}.",
    "يجب أن يكون {PropertyName} أصغر من {Threshold}.",
    "يجب أن يكون {PropertyName} أصغر من أو يساوي {Threshold}.",
    "يجب أن يكون {PropertyName} بين {Min} و{Max}.",
    "يجب أن يكون {PropertyName} بين {Min} و{Max} حصريًا.",
    "يجب أن يكون {PropertyName} بين {Min} و{Max}.",
    "يجب أن يكون {PropertyName} واحدًا من: {Values}.",
    "يجب ألا يكون {PropertyName} واحدًا من: {Values}.",
    "يجب أن يحتوي {PropertyName} على {Precision} رقمًا بحد أقصى مع {Scale} منازل عشرية.",
    "يجب أن يكون {PropertyName} بعد {Threshold}.",
    "يجب أن يكون {PropertyName} قبل {Threshold}."
];

function createPack(messages) {
    return Object.fromEntries(messageKeys.map((key, index) => [key, messages[index]]));
}

export const defaultMessages = {
    en: createPack(englishMessages),
    ar: createPack(arabicMessages)
};
