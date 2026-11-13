//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Sample.Console.Domain.ValueObjects;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly record struct AgeValueRecordStruct(int Value) : IComparable<AgeValueRecordStruct>, IComparable {
    private readonly string GetDebuggerDisplay() => ToString();

    public bool IsInfant() => Value is >= 0 and < 3;
    public bool IsTeenage() => Value is > 12 and < 19;
    public int CompareTo(AgeValueRecordStruct other) => Value.CompareTo(other.Value);

    int IComparable.CompareTo(object? obj) {
        if(obj is AgeValueRecordStruct other) {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(AgeValueRecordStruct)}.", nameof(obj));
    }

    public static bool operator <(AgeValueRecordStruct left, AgeValueRecordStruct right) => left.CompareTo(right) < 0;

    public static bool operator <=(AgeValueRecordStruct left, AgeValueRecordStruct right) => left.CompareTo(right) <= 0;

    public static bool operator >(AgeValueRecordStruct left, AgeValueRecordStruct right) => left.CompareTo(right) > 0;

    public static bool operator >=(AgeValueRecordStruct left, AgeValueRecordStruct right) => left.CompareTo(right) >= 0;

    public static implicit operator int(AgeValueRecordStruct value) => value.Value;
    public static implicit operator int?(AgeValueRecordStruct value) => value.Value;
    public static implicit operator AgeValueRecordStruct(int value) => new(value);
    public static implicit operator AgeValueRecordStruct(int? value) => new(value ?? -1);
}

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public record class AgeValueRecordClass(int Value) : IComparable<AgeValueRecordClass>, IComparable {
    private string GetDebuggerDisplay() => ToString();

    public bool IsInfant() => Value is >= 0 and < 3;
    public bool IsTeenage() => Value is > 12 and < 19;

    public int CompareTo(AgeValueRecordClass? other) => other is null ? 1 : Value.CompareTo(other.Value);

    int IComparable.CompareTo(object? obj) {
        if(obj is null) {
            return 1;
        }

        if(obj is AgeValueRecordClass other) {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(AgeValueRecordClass)}.", nameof(obj));
    }

    public static bool operator <(AgeValueRecordClass left, AgeValueRecordClass right) {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(AgeValueRecordClass left, AgeValueRecordClass right) {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(AgeValueRecordClass left, AgeValueRecordClass right) {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(AgeValueRecordClass left, AgeValueRecordClass right) {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) >= 0;
    }

    public static implicit operator int(AgeValueRecordClass value) {
        ArgumentNullException.ThrowIfNull(value);
        return value.Value;
    }
    public static implicit operator int?(AgeValueRecordClass value) {
        ArgumentNullException.ThrowIfNull(value);
        return value.Value;
    }
    public static implicit operator AgeValueRecordClass(int value) => new(value);
    public static implicit operator AgeValueRecordClass(int? value) => new(value ?? -1);
}
