namespace Niklasifiera.Test;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Diagnostics.CodeAnalysis;

using Niklasifiera.Services;
using Microsoft.CodeAnalysis.CSharp.Testing;

using VerifyCS = CSharpCodeFixVerifier<NiklasifieraAnalyzer, NiklasifieraCodeFixProvider>;

using static TestData;

[TestClass]
public class NiklasifieraUnitTest
{
    // Tests that should produce no diagnostics
    [TestMethod]
    [DataRow(EmptyCode, DisplayName = "Empty code")]
    [DataRow(MethodWithNoParameters, DisplayName = "Method with no parameters")]
    [DataRow(MethodWithOneParameter, DisplayName = "Method with one parameter")]
    [DataRow(ConstructorWithNoParameters, DisplayName = "Constructor with no parameters")]
    [DataRow(ConstructorWithOneParameter, DisplayName = "Constructor with one parameter")]
    [DataRow(GenericMethodWithOneParameter, DisplayName = "Generic method with one parameter")]
    [DataRow(MethodWithMultipleParametersCorrectlyFormatted, DisplayName = "Method with multiple parameters correctly formatted")]
    [DataRow(PrimaryConstructorWithInheritanceCorrectlyFormatted, DisplayName = "Primary constructor with inheritance correctly formatted")]
    [DataRow(SingleInheritanceCorrectlyFormatted, DisplayName = "Single inheritance correctly formatted")]
    [DataRow(SingleInheritanceWithPrimaryConstructorCorrectlyFormatted, DisplayName = "Single inheritance with primary constructor correctly formatted")]
    [DataRow(RecordWithOneParameter, DisplayName = "Record with one parameter")]
    [DataRow(RecordWithTwoParametersCorrectlyFormatted, DisplayName = "Record with multiple parameters correctly formatted")]
    [DataRow(ConditionalOperatorReturnCorrectlyFormatted, DisplayName = "Conditional operator in return correctly formatted")]
    [DataRow(ConditionalOperatorAssignmentCorrectlyFormatted, DisplayName = "Conditional operator in assignment correctly formatted")]
    [DataRow(NestedConditionalCorrectlyFormatted, DisplayName = "Nested conditional operator correctly formatted")]
    [DataRow(NestedConditionalInAssignmentCorrectlyFormatted, DisplayName = "Nested conditional in assignment correctly formatted")]
    public async Task NoDiagnostic_Tests
        (
        [StringSyntax("c#-test")] string testCode
        )
        => await VerifyCS
            .VerifyAnalyzerAsync(testCode);

    // Tests for signature diagnostics (methods/constructors with multiple parameters on single line)
    [TestMethod]
    [DynamicData(nameof(GetSignatureTestCases), DynamicDataSourceType.Method)]
    public async Task SignatureDiagnostic_Tests(DiagnosticTestCase testCase)
    {
        var diagnostic =
            DiagnosticHelper
                .Signature()
                .WithLocation(0)
                .WithArguments(testCase.ExpectedIdentifier);

        await VerifyCodeFixWithDiagnosticAsync(diagnostic, testCase.TestCode, testCase.FixedCode);
    }

    private static IEnumerable<object[]> GetSignatureTestCases()
    {
        yield return [new DiagnosticTestCase(MethodWithTwoParametersOnSingleLine, MethodWithTwoParametersOnSingleLineFixed, "TestMethod")];
        yield return [new DiagnosticTestCase(ConstructorWithTwoParametersOnSingleLine, ConstructorWithTwoParametersOnSingleLineFixed, "TestClass")];
        yield return [new DiagnosticTestCase(MethodWithThreeParametersOnSingleLine, MethodWithThreeParametersOnSingleLineFixed, "TestMethod")];
        yield return [new DiagnosticTestCase(GenericMethodWithMultipleParametersOnSingleLine, GenericMethodWithMultipleParametersOnSingleLineFixed, "ExecuteAsync")];
        yield return [new DiagnosticTestCase(MethodWithGenericConstraintsOnSingleLine, MethodWithGenericConstraintsOnSingleLineFixed, "ProcessData")];
        yield return [new DiagnosticTestCase(RecordWithTwoParametersOnSingleLine, RecordWithTwoParametersOnSingleLineFixed, "Person")];
    }

    // Special case for primary constructor with inheritance (different location)
    [TestMethod]
    public async Task PrimaryConstructorWithInheritanceOnSingleLine_Diagnostic()
    {
        var expected =
            DiagnosticHelper
                .Signature()
                .WithLocation(7, 30)
                .WithArguments("SampleClient");

        await VerifyCS
            .VerifyCodeFixAsync(PrimaryConstructorWithInheritanceOnSingleLine, expected, PrimaryConstructorWithInheritanceOnSingleLineFixed);
    }

    // Tests for inheritance diagnostics
    [TestMethod]
    [DynamicData(nameof(GetInheritanceTestCases), DynamicDataSourceType.Method)]
    public async Task InheritanceDiagnostic_Tests(DiagnosticTestCase testCase)
    {
        var diagnostic =
            DiagnosticHelper
                .Inheritance()
                .WithLocation(0)
                .WithArguments(testCase.ExpectedIdentifier);

        await VerifyCodeFixWithDiagnosticAsync(diagnostic, testCase.TestCode, testCase.FixedCode);
    }

    private static IEnumerable<object[]> GetInheritanceTestCases()
    {
        yield return [new DiagnosticTestCase(SingleInheritanceOnSameLine, SingleInheritanceOnSameLineFixed, "TestClass")];
        yield return [new DiagnosticTestCase(SingleInheritanceWithPrimaryConstructorOnSameLine, SingleInheritanceWithPrimaryConstructorOnSameLineFixed, "TestClass")];
        yield return [new DiagnosticTestCase(SingleInheritanceWithBadIndentation, SingleInheritanceWithBadIndentationExpectedFixed, "TestClass")];
    }

    // Tests for conditional operator diagnostics
    [TestMethod]
    [DynamicData(nameof(GetConditionalOperatorTestCases), DynamicDataSourceType.Method)]
    public async Task ConditionalOperatorDiagnostic_Tests(ConditionalOperatorTestCase testCase)
    {
        var expected =
            DiagnosticHelper
                .ConditionalOperator()
                .WithLocation(0);

        await VerifyCS
            .VerifyCodeFixAsync(testCase.TestCode, expected, testCase.FixedCode);
    }

    private static IEnumerable<object[]> GetConditionalOperatorTestCases()
    {
        yield return [new ConditionalOperatorTestCase(ConditionalOperatorSingleLine, ConditionalOperatorSingleLineFixed)];
        yield return [new ConditionalOperatorTestCase(ConditionalOperatorAssignmentSingleLine, ConditionalOperatorAssignmentSingleLineFixed)];
        yield return [new ConditionalOperatorTestCase(ConditionalOperatorPartialSplit, ConditionalOperatorPartialSplitFixed)];
        yield return [new ConditionalOperatorTestCase(ConditionalOperatorWrongIndentation, ConditionalOperatorWrongIndentationFixed)];
    }

    // Tests for nested conditional operator diagnostics (multiple diagnostics expected)
    [TestMethod]
    public async Task NestedConditionalSingleLine_Diagnostic()
    {
        var expectedOuter =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(9, 20, 9, 63);

        var expectedInner =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(9, 34, 9, 56);

        await VerifyNestedConditionalFixAsync(NestedConditionalSingleLine, [expectedOuter, expectedInner], NestedConditionalSingleLineFixed);
    }

    [TestMethod]
    public async Task NestedConditionalInAssignment_Diagnostic()
    {
        var expectedOuter =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(9, 26, 9, 69);

        var expectedInner =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(9, 40, 9, 62);

        await VerifyNestedConditionalFixAsync(NestedConditionalInAssignment, [expectedOuter, expectedInner], NestedConditionalInAssignmentFixed);
    }

    [TestMethod]
    public async Task DeeplyNestedConditional_Diagnostic()
    {
        var expected1 =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(10, 20, 10, 60);

        var expected2 =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(10, 26, 10, 53);

        var expected3 =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(10, 32, 10, 46);

        await VerifyNestedConditionalFixAsync(DeeplyNestedConditional, [expected1, expected2, expected3], DeeplyNestedConditionalFixed, numberOfFixAllIterations: 3);
    }

    [TestMethod]
    public async Task NestedConditionalInFalseBranch_Diagnostic()
    {
        var expectedOuter =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(9, 20, 9, 63);

        var expectedInner =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(9, 40, 9, 62);

        await VerifyNestedConditionalFixAsync(NestedConditionalInFalseBranch, [expectedOuter, expectedInner], NestedConditionalInFalseBranchFixed);
    }

    [TestMethod]
    public async Task NestedConditionalPartiallyFormatted_Diagnostic()
    {
        var expectedInner =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(10, 20, 10, 42);

        await VerifyCS
            .VerifyCodeFixAsync(NestedConditionalPartiallyFormatted, expectedInner, NestedConditionalPartiallyFormattedFixed);
    }

    [TestMethod]
    public async Task NestedConditionalWithComplexCondition_Diagnostic()
    {
        var expectedOuter =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(8, 20, 8, 101);

        var expectedInner =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(8, 54, 8, 94);

        await VerifyNestedConditionalFixAsync(NestedConditionalWithComplexCondition, [expectedOuter, expectedInner], NestedConditionalWithComplexConditionFixed);
    }

    [TestMethod]
    public async Task NestedConditionalWithoutParensMultiLineCondition_Diagnostic()
    {
        // This is the exact scenario from the sample code
        // It produces 2 diagnostics because both the outer and nested conditionals are on single lines
        var expectedOuter =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(9, 17, 13, 31);

        var expectedNested =
            DiagnosticHelper
                .ConditionalOperator()
                .WithSpan(10, 45, 12, 35);

        // With the multi-line condition fix, this now only needs 1 iteration
        await VerifyNestedConditionalFixAsync(
            NestedConditionalWithoutParensMultiLineCondition,
            [expectedOuter, expectedNested],
            NestedConditionalWithoutParensMultiLineConditionFixed,
            numberOfFixAllIterations: 1);
    }

    // Helper method for nested conditional tests that need multiple fix iterations
    private static async Task VerifyNestedConditionalFixAsync
        (
        string testCode,
        DiagnosticResult[] diagnostics,
        string fixedCode,
        int numberOfFixAllIterations = 2
        )
    {
        var test =
            new CSharpCodeFixTest<NiklasifieraAnalyzer, NiklasifieraCodeFixProvider, DefaultVerifier>
            {
                TestCode = testCode.ReplaceLineEndings(),
                FixedCode = fixedCode.ReplaceLineEndings(),
                NumberOfFixAllIterations = numberOfFixAllIterations
            };

        test.ExpectedDiagnostics
            .AddRange(diagnostics);

        await test
            .RunAsync();
    }

    // Tests for trivia preservation in inheritance formatting
    [TestMethod]
    public async Task InheritanceWithCommentsOnColon_PreservesTrivia()
        => await VerifyInheritanceTriviaPreservationAsync
        (
            InheritanceWithCommentsOnColonLine,
            InheritanceWithCommentsOnColonPreserved,
            "SampleClient6"
        );

    [TestMethod]
    public async Task InheritanceWithInlineComments_IntelligentlyRepositionsComments()
        => await VerifyInheritanceTriviaPreservationAsync
        (
            InheritanceWithComments,
            InheritanceWithCommentsIntelligentlyRepositioned,
            "TestClass"
        );

    // Helper method to reduce test duplication
    private static async Task VerifyCodeFixWithDiagnosticAsync
        (
        DiagnosticResult diagnostic,
        string testCode,
        string fixedCode
        )
        => await VerifyCS
            .VerifyCodeFixAsync(testCode, diagnostic, fixedCode);

    // Helper method for trivia preservation tests
    private static async Task VerifyInheritanceTriviaPreservationAsync
        (
        string testCode,
        string fixedCode,
        string argumentValue
        )
    {
        var mockConfig =
            new MockConfigurationService
            (
                triviaHandling: TriviaHandlingBehavior.Preserve,
                indentationUnit: "    ",
                lineEnding: "\r\n"
            );

        var diagnostic =
            DiagnosticHelper
                .Inheritance()
                .WithLocation(0)
                .WithArguments(argumentValue);

        TestableNiklasifieraCodeFixProvider.MockConfigurationService = mockConfig;

        try
        {
            var test =
                new CSharpCodeFixTest<NiklasifieraAnalyzer, TestableNiklasifieraCodeFixProvider, DefaultVerifier>
                {
                    TestCode = testCode.ReplaceLineEndings(),
                    FixedCode = fixedCode.ReplaceLineEndings()
                };

            test.ExpectedDiagnostics
                .Add(diagnostic);

            await test
                .RunAsync();
        }
        finally
        {
            TestableNiklasifieraCodeFixProvider.MockConfigurationService = null;
        }
    }

    // NOTE: This test demonstrates intelligent comment repositioning functionality
    // but cannot run automatically due to test framework limitations with .editorconfig settings.
    // The functionality works correctly when niklasifiera_preserve_trivia = preserve is set.
    //
    // [TestMethod]
    // public async Task InheritanceFormattingWithComments_PreserveTrivia_IntelligentlyRepositionsComments()
    // {
    //     // This test verifies that when niklasifiera_preserve_trivia = preserve,
    //     // comments are intelligently repositioned rather than kept in place
    //     
    //     var testCode = InheritanceWithComments;
    //     var expectedCode = InheritanceWithCommentsIntelligentlyRepositioned;
    //     
    //     var expected = InheritanceDiagnostic()
    //         .WithLocation(0)
    //         .WithArguments("TestClass");
    //
    //     await VerifyCS
    //         .VerifyCodeFixAsync(testCode, expected, expectedCode);
    // }

    // NOTE: Trivia preservation tests would require setting up .editorconfig
    // or modifying the test framework to support analyzer configuration.
    // For now, these test cases serve as documentation of the expected behavior.

    // When niklasifiera_preserve_trivia = skip (default):
    // - Code fixes should not be offered for MethodWithCommentsAndTwoParameters
    // - Code fixes should not be offered for InheritanceWithComments

    // When niklasifiera_preserve_trivia = preserve:
    // - MethodWithCommentsAndTwoParameters should be fixed to MethodWithCommentsPreserved
    // - InheritanceWithComments should be fixed to InheritanceWithCommentsIntelligentlyRepositioned
}

#region Test Infrastructure for Mocking

// Helper class to get diagnostic descriptors
public static class DiagnosticHelper
{
    private static readonly NiklasifieraAnalyzer Analyzer =
        new();

    public static DiagnosticResult GetDiagnostic(int ruleIndex)
    {
        var rule =
            Analyzer.SupportedDiagnostics[ruleIndex];

        return VerifyCS.Diagnostic(rule);
    }

    public static DiagnosticResult Signature()
        => GetDiagnostic(0);

    public static DiagnosticResult Inheritance()
        => GetDiagnostic(1);

    public static DiagnosticResult ConditionalOperator()
        => GetDiagnostic(2);
}

// Test data class for conditional operator tests (no identifier needed)
public sealed class ConditionalOperatorTestCase
    (
    string testCode,
    string fixedCode
    )
{
    public string TestCode { get; init; } = testCode;
    public string FixedCode { get; init; } = fixedCode;
}

// Test data class to reduce primitive obsession
public sealed class DiagnosticTestCase
    (
    string testCode,
    string fixedCode,
    string expectedIdentifier
    )
{
    public string TestCode { get; init; } = testCode;
    public string FixedCode { get; init; } = fixedCode;
    public string ExpectedIdentifier { get; init; } = expectedIdentifier;
}

/// <summary>
/// Mock implementation of IConfigurationService for testing
/// </summary>
internal class MockConfigurationService
    (
    TriviaHandlingBehavior triviaHandling = TriviaHandlingBehavior.Skip,
    string indentationUnit = "    ",
    string lineEnding = "\r\n"
    )
    : IConfigurationService
{
    private readonly TriviaHandlingBehavior _triviaHandling = triviaHandling;
    private readonly string _indentationUnit = indentationUnit;
    private readonly string _lineEnding = lineEnding;

    public Task<TriviaHandlingBehavior> GetTriviaHandlingBehaviorAsync(Microsoft.CodeAnalysis.Document document)
        => Task.FromResult(_triviaHandling);

    public Task<string> GetIndentationUnitAsync(Microsoft.CodeAnalysis.Document document)
        => Task.FromResult(_indentationUnit);

    public Task<string> GetLineEndingAsync(Microsoft.CodeAnalysis.Document document)
        => Task.FromResult(_lineEnding);
}

/// <summary>
/// Testable version of NiklasifieraCodeFixProvider that uses a static mock configuration service
/// </summary>
internal class TestableNiklasifieraCodeFixProvider
    : NiklasifieraCodeFixProvider
{
    internal static IConfigurationService MockConfigurationService { get; set; }

    public TestableNiklasifieraCodeFixProvider()
        : base(CreateCodeFixServices())
    {
    }

    private static ICodeFixService[] CreateCodeFixServices()
    {
        var configService =
            MockConfigurationService
                ?? new MockConfigurationService();

        return
        [
            new SignatureFormattingService(configService),
            new InheritanceFormattingService(configService),
            new ConditionalOperatorFormattingService(configService)
        ];
    }
}

#endregion
