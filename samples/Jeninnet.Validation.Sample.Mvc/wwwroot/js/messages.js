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
export const defaultMessages = {
    en: {
        NotNull: "{PropertyName} is required.",
        NotEmpty: "{PropertyName} is required.",
        StringNotEmpty: "{PropertyName} is required.",
        Length: "{PropertyName} must be between {Min} and {Max} characters.",
        ExactLength: "{PropertyName} must be exactly {Length} characters.",
        Regex: "{PropertyName} has an invalid format.",
        Email: "{PropertyName} must be a valid email address.",
        Url: "{PropertyName} must be a valid URL.",
        IPAddress: "{PropertyName} must be a valid IP address.",
        Phone: "{PropertyName} must be a valid phone number.",
        CreditCard: "{PropertyName} must be a valid card number.",
        Base64String: "{PropertyName} must be valid base64.",
        FileExtensions: "{PropertyName} has an unsupported file extension.",
        Contains: "{PropertyName} must contain {Value}.",
        StartsWith: "{PropertyName} must start with {Value}.",
        EndsWith: "{PropertyName} must end with {Value}.",
        Count: "{PropertyName} must contain between {Min} and {Max} items.",
        MinCount: "{PropertyName} must contain at least {Min} items.",
        MaxCount: "{PropertyName} must contain at most {Max} items.",
        GreaterThan: "{PropertyName} must be greater than {Threshold}.",
        GreaterThanOrEqual: "{PropertyName} must be at least {Threshold}.",
        LessThan: "{PropertyName} must be less than {Threshold}.",
        LessThanOrEqual: "{PropertyName} must be at most {Threshold}.",
        InclusiveBetween: "{PropertyName} must be between {Min} and {Max}.",
        ExclusiveBetween: "{PropertyName} must be strictly between {Min} and {Max}.",
        Range: "{PropertyName} must be between {Min} and {Max}.",
        In: "{PropertyName} must be one of: {Values}.",
        NotIn: "{PropertyName} must not be one of: {Values}.",
        ScalePrecision: "{PropertyName} must have at most {Precision} digits with scale {Scale}.",
        After: "{PropertyName} must be after {Threshold}.",
        Before: "{PropertyName} must be before {Threshold}."
    },
    ar: {
        NotNull: "{PropertyName} مطلوب.",
        NotEmpty: "{PropertyName} مطلوب.",
        StringNotEmpty: "{PropertyName} مطلوب.",
        Length: "يجب أن يكون طول {PropertyName} بين {Min} و{Max} حرفًا.",
        ExactLength: "يجب أن يكون طول {PropertyName} مساويًا لـ {Length} حرفًا.",
        Regex: "تنسيق {PropertyName} غير صالح.",
        Email: "يجب أن يكون {PropertyName} بريدًا إلكترونيًا صالحًا.",
        Url: "يجب أن يكون {PropertyName} رابطًا صالحًا.",
        IPAddress: "يجب أن يكون {PropertyName} عنوان IP صالحًا.",
        Phone: "يجب أن يكون {PropertyName} رقم هاتف صالحًا.",
        CreditCard: "يجب أن يكون {PropertyName} رقم بطاقة صالحًا.",
        Base64String: "يجب أن يكون {PropertyName} بترميز base64 صالح.",
        FileExtensions: "امتداد ملف {PropertyName} غير مدعوم.",
        Contains: "يجب أن يحتوي {PropertyName} على {Value}.",
        StartsWith: "يجب أن يبدأ {PropertyName} بـ {Value}.",
        EndsWith: "يجب أن ينتهي {PropertyName} بـ {Value}.",
        Count: "يجب أن يحتوي {PropertyName} على عدد عناصر بين {Min} و{Max}.",
        MinCount: "يجب أن يحتوي {PropertyName} على {Min} عنصرًا على الأقل.",
        MaxCount: "يجب أن يحتوي {PropertyName} على {Max} عنصر كحد أقصى.",
        GreaterThan: "يجب أن يكون {PropertyName} أكبر من {Threshold}.",
        GreaterThanOrEqual: "يجب أن يكون {PropertyName} أكبر من أو يساوي {Threshold}.",
        LessThan: "يجب أن يكون {PropertyName} أصغر من {Threshold}.",
        LessThanOrEqual: "يجب أن يكون {PropertyName} أصغر من أو يساوي {Threshold}.",
        InclusiveBetween: "يجب أن يكون {PropertyName} بين {Min} و{Max}.",
        ExclusiveBetween: "يجب أن يكون {PropertyName} بين {Min} و{Max} حصريًا.",
        Range: "يجب أن يكون {PropertyName} بين {Min} و{Max}.",
        In: "يجب أن يكون {PropertyName} واحدًا من: {Values}.",
        NotIn: "يجب ألا يكون {PropertyName} واحدًا من: {Values}.",
        ScalePrecision: "يجب أن يحتوي {PropertyName} على {Precision} رقمًا بحد أقصى مع {Scale} منازل عشرية.",
        After: "يجب أن يكون {PropertyName} بعد {Threshold}.",
        Before: "يجب أن يكون {PropertyName} قبل {Threshold}."
    }
};
