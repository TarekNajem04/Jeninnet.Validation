//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

public enum TestRole {
    Admin = 1,
    User = 2
}

[Flags]
public enum TestAccess {
    None = 0,
    Read = 1,
    Write = 1 << 1,
    Delete = 1 << 2,
    All = Read | Write | Delete
}

[Flags]
public enum LongAccess : long {
    None = 0,
    Read = 1,
    Write = 1L << 40,
    All = Read | Write
}

[Flags]
public enum ULongAccess : ulong {
    None = 0,
    Read = 1,
    Write = 1UL << 40,
    All = Read | Write
}

file sealed class EnumModel {
    public TestRole Role { get; init; }
    public TestRole? OptionalRole { get; init; }
}

file sealed class FlagsEnumModel {
    public TestAccess Access { get; init; }
    public TestAccess? OptionalAccess { get; init; }
}

file sealed class WideFlagsModel {
    public LongAccess Long { get; init; }
    public ULongAccess ULong { get; init; }
}

[TestClass]
public sealed class EnumRuleTests {
    [TestMethod]
    public void DefinedValue_Passes() {
        var result = Run(TestRole.Admin);
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void UndefinedValue_Fails() {
        var result = Run((TestRole)99);
        ValidationAssert.HasError(result, nameof(EnumModel.Role));
    }

    [TestMethod]
    public void NullableEnum_WhenNull_Skips() {
        var rule = new EnumRule<EnumModel, TestRole?>(
            ValidatorFactory.Accessor<EnumModel, TestRole?>(x => x.OptionalRole, nameof(EnumModel.OptionalRole)),
            typeof(TestRole));

        var result = RuleRunner.Run(rule, new EnumModel { OptionalRole = null });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void NullableEnum_WhenValueDefined_Passes() {
        var rule = new EnumRule<EnumModel, TestRole?>(
            ValidatorFactory.Accessor<EnumModel, TestRole?>(x => x.OptionalRole, nameof(EnumModel.OptionalRole)),
            typeof(TestRole));

        var result = RuleRunner.Run(rule, new EnumModel { OptionalRole = TestRole.User });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void Flags_CombinationOfDeclaredValues_Passes() {
        var result = RunFlags(TestAccess.Read | TestAccess.Write);

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void Flags_AllDeclaredValues_Passes() {
        var result = RunFlags(TestAccess.All);

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void Flags_Zero_Passes() {
        var result = RunFlags(TestAccess.None);

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void Flags_UndefinedBit_Fails() {
        var result = RunFlags((TestAccess)8);

        ValidationAssert.HasError(result, nameof(FlagsEnumModel.Access));
    }

    [TestMethod]
    public void Flags_NullableCombination_Passes() {
        var rule = new EnumRule<FlagsEnumModel, TestAccess?>(
            ValidatorFactory.Accessor<FlagsEnumModel, TestAccess?>(x => x.OptionalAccess, nameof(FlagsEnumModel.OptionalAccess)),
            typeof(TestAccess));

        var result = RuleRunner.Run(rule, new FlagsEnumModel { OptionalAccess = TestAccess.Read | TestAccess.Delete });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void Flags_NullableUndefinedBit_Fails() {
        var rule = new EnumRule<FlagsEnumModel, TestAccess?>(
            ValidatorFactory.Accessor<FlagsEnumModel, TestAccess?>(x => x.OptionalAccess, nameof(FlagsEnumModel.OptionalAccess)),
            typeof(TestAccess));

        var result = RuleRunner.Run(rule, new FlagsEnumModel { OptionalAccess = (TestAccess)16 });

        ValidationAssert.HasError(result, nameof(FlagsEnumModel.OptionalAccess));
    }

    [TestMethod]
    public void Flags_LongBackedCombination_Passes() {
        var rule = new EnumRule<WideFlagsModel, LongAccess>(
            ValidatorFactory.Accessor<WideFlagsModel, LongAccess>(x => x.Long, nameof(WideFlagsModel.Long)),
            typeof(LongAccess));

        var result = RuleRunner.Run(rule, new WideFlagsModel { Long = LongAccess.Read | LongAccess.Write });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void Flags_ULongBackedCombination_Passes() {
        var rule = new EnumRule<WideFlagsModel, ULongAccess>(
            ValidatorFactory.Accessor<WideFlagsModel, ULongAccess>(x => x.ULong, nameof(WideFlagsModel.ULong)),
            typeof(ULongAccess));

        var result = RuleRunner.Run(rule, new WideFlagsModel { ULong = ULongAccess.Read | ULongAccess.Write });

        ValidationAssert.IsValid(result);
    }

    private static ValidationResult RunFlags(TestAccess value) {
        var rule = new EnumRule<FlagsEnumModel, TestAccess>(
            ValidatorFactory.Accessor<FlagsEnumModel, TestAccess>(x => x.Access, nameof(FlagsEnumModel.Access)),
            typeof(TestAccess));
        return RuleRunner.Run(rule, new FlagsEnumModel { Access = value });
    }

    private static ValidationResult Run(TestRole value) {
        var rule = new EnumRule<EnumModel, TestRole>(
            ValidatorFactory.Accessor<EnumModel, TestRole>(x => x.Role, nameof(EnumModel.Role)),
            typeof(TestRole));
        return RuleRunner.Run(rule, new EnumModel { Role = value });
    }
}
