namespace Niklasifiera.Test;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Diagnostics.CodeAnalysis;

using Niklasifiera.Services;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

using VerifyCS = CSharpCodeFixVerifier<NiklasifieraAnalyzer, NiklasifieraCodeFixProvider>;

[TestClass]
public class NiklasifieraUnitTest
{
    #region Test Case Data - No Diagnostics

    [StringSyntax("c#-test")]
    private const string EmptyCode = "";

    [StringSyntax("c#-test")]
    private const string MethodWithNoParameters =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public void TestMethod()
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithOneParameter =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public void TestMethod(int parameter)
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithMultipleParametersCorrectlyFormatted =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public void TestMethod
                    (
                    int parameter1,
                    string parameter2
                    )
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConstructorWithNoParameters =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public TestClass()
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConstructorWithOneParameter =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public TestClass(int parameter)
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string GenericMethodWithOneParameter =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public T Process<T>(T input) where T : class
                {
                    return input;
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string PrimaryConstructorWithInheritanceCorrectlyFormatted =
        """
        using System;
        using System.Net.Http;
        using System.Threading.Tasks;
        
        namespace TestNamespace
        {
            public class SampleClient
                (
                HttpClient httpClient,
                Action<string> log
                )
                : IAsyncDisposable
                , IDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => new ValueTask();
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string SingleInheritanceCorrectlyFormatted =
        """
        using System;
        
        namespace TestNamespace
        {
            public class TestClass
                : IDisposable
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string SingleInheritanceWithPrimaryConstructorCorrectlyFormatted =
        """
        using System;
        using System.Data;
        
        namespace TestNamespace
        {
            public class TestClass
                (
                IDbConnection DbConnection,
                IDbTransaction DbTransaction
                )
                : IDisposable
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string RecordWithTwoParametersCorrectlyFormatted =
        """
        namespace TestNamespace
        {
            public record Person
                (
                string FirstName,
                string LastName
                );
        }
        """;

    [StringSyntax("c#-test")]
    private const string RecordWithOneParameter =
        """
        namespace TestNamespace
        {
            public record Person(string Name);
        }
        """;

    #endregion

    #region Test Case Data - Signature Diagnostics

    [StringSyntax("c#-test")]
    private const string MethodWithTwoParametersOnSingleLine =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public void TestMethod{|#0:(int parameter1, string parameter2)|}
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithTwoParametersOnSingleLineFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public void TestMethod
                    (
                    int parameter1,
                    string parameter2
                    )
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConstructorWithTwoParametersOnSingleLine =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public TestClass{|#0:(int parameter1, string parameter2)|}
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConstructorWithTwoParametersOnSingleLineFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public TestClass
                    (
                    int parameter1,
                    string parameter2
                    )
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithThreeParametersOnSingleLine =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public void TestMethod{|#0:(int p1, string p2, bool p3)|}
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithThreeParametersOnSingleLineFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public void TestMethod
                    (
                    int p1,
                    string p2,
                    bool p3
                    )
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string GenericMethodWithMultipleParametersOnSingleLine =
        """
        using System.Net.Http;
        using System.Threading;
        using System.Threading.Tasks;
        
        namespace TestNamespace
        {
            public class TestClass
            {
                public async Task ExecuteAsync<T>{|#0:(T request, CancellationToken cancellationToken)|}
                    where T : HttpRequestMessage
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string GenericMethodWithMultipleParametersOnSingleLineFixed =
        """
        using System.Net.Http;
        using System.Threading;
        using System.Threading.Tasks;
        
        namespace TestNamespace
        {
            public class TestClass
            {
                public async Task ExecuteAsync<T>
                    (
                    T request,
                    CancellationToken cancellationToken
                    )
                    where T : HttpRequestMessage
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string PrimaryConstructorWithInheritanceOnSingleLine =
        """
        using System;
        using System.Net.Http;
        using System.Threading.Tasks;
        
        namespace TestNamespace
        {
            public class SampleClient(HttpClient httpClient, Action<string> log)
                : IAsyncDisposable
                , IDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => new ValueTask();
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string PrimaryConstructorWithInheritanceOnSingleLineFixed =
        """
        using System;
        using System.Net.Http;
        using System.Threading.Tasks;
        
        namespace TestNamespace
        {
            public class SampleClient
                (
                HttpClient httpClient,
                Action<string> log
                )
                : IAsyncDisposable
                , IDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => new ValueTask();
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithGenericConstraintsOnSingleLine =
        """
        namespace TestNamespace
        {
            public interface IProcessor<T> { }
            
            public class TestClass
            {
                public void ProcessData<T, U>{|#0:(T data, U processor, string format)|}
                    where T : class, new()
                    where U : IProcessor<T>
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithGenericConstraintsOnSingleLineFixed =
        """
        namespace TestNamespace
        {
            public interface IProcessor<T> { }
            
            public class TestClass
            {
                public void ProcessData<T, U>
                    (
                    T data,
                    U processor,
                    string format
                    )
                    where T : class, new()
                    where U : IProcessor<T>
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string RecordWithTwoParametersOnSingleLine =
        """
        namespace TestNamespace
        {
            public record Person{|#0:(string FirstName, string LastName)|};
        }
        """;

    [StringSyntax("c#-test")]
    private const string RecordWithTwoParametersOnSingleLineFixed =
        """
        namespace TestNamespace
        {
            public record Person
                (
                string FirstName,
                string LastName
                );
        }
        """;

    #endregion

    #region Test Case Data - Inheritance Diagnostics

    [StringSyntax("c#-test")]
    private const string SingleInheritanceOnSameLine =
        """
        using System;

        namespace TestNamespace
        {
            public class TestClass {|#0:: IDisposable|}
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string SingleInheritanceOnSameLineFixed =
        """
        using System;

        namespace TestNamespace
        {
            public class TestClass
                : IDisposable
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string SingleInheritanceWithPrimaryConstructorOnSameLine =
        """
        using System;
        using System.Data;

        namespace TestNamespace
        {
            public class TestClass
                (
                IDbConnection DbConnection,
                IDbTransaction DbTransaction
                ){|#0:: IDisposable|}
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string SingleInheritanceWithPrimaryConstructorOnSameLineFixed =
        """
        using System;
        using System.Data;

        namespace TestNamespace
        {
            public class TestClass
                (
                IDbConnection DbConnection,
                IDbTransaction DbTransaction
                )
                : IDisposable
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string SingleInheritanceWithBadIndentation =
        """
        using System;

        namespace TestNamespace
        {
            public class TestClass
                 {|#0:: IDisposable|}
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string SingleInheritanceWithBadIndentationExpectedFixed =
        """
        using System;

        namespace TestNamespace
        {
            public class TestClass
                : IDisposable
            {
                public void Dispose() { }
            }
        }
        """;

    #endregion

    #region Test Case Data - Conditional Operator Diagnostics

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorSingleLine =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return {|#0:condition ? "Yes" : "No"|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorSingleLineFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return condition
                        ? "Yes"
                        : "No";
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorAssignmentSingleLine =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    var result = {|#0:condition ? "Yes" : "No"|};
                    return result;
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorAssignmentSingleLineFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    var result =
                        condition
                            ? "Yes"
                            : "No";
                    return result;
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorPartialSplit =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return {|#0:condition ?
                        "Yes" :
                        "No"|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorPartialSplitFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return condition
                        ? "Yes"
                        : "No";
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorWrongIndentation =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return {|#0:condition
                    ? "Yes"
                    : "No"|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorWrongIndentationFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return condition
                        ? "Yes"
                        : "No";
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorReturnCorrectlyFormatted =
        """
        namespace TestNamespace;

        public class TestClass
        {
            public string TestMethod()
            {
                var condition = true;
                return condition
                    ? "Yes"
                    : "No";
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string ConditionalOperatorAssignmentCorrectlyFormatted =
        """
        namespace TestNamespace;

        public class TestClass
        {
            public string TestMethod()
            {
                var condition = true;
                var result =
                    condition
                        ? "Yes"
                        : "No";
                return result;
            }
        }
        """;

    #endregion

    // Helper class to get diagnostic descriptors
    private static class DiagnosticHelper
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
    public async Task NoDiagnostic_Tests
        (
        [StringSyntax("c#-test")] string testCode
        )
        => await VerifyCS
            .VerifyAnalyzerAsync(testCode);

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

    // Tests for signature diagnostics (methods/constructors with multiple parameters on single line)
    [TestMethod]
    [DynamicData(nameof(GetSignatureTestCases), DynamicDataSourceType.Method)]
    public async Task SignatureDiagnostic_Tests(DiagnosticTestCase testCase)
    {
        var expected =
            DiagnosticHelper
                .Signature()
                .WithLocation(0)
                .WithArguments(testCase.ExpectedIdentifier);

        await VerifyCS
            .VerifyCodeFixAsync(testCase.TestCode, expected, testCase.FixedCode);
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
        var expected =
            DiagnosticHelper
                .Inheritance()
                .WithLocation(0)
                .WithArguments(testCase.ExpectedIdentifier);

        await VerifyCS
            .VerifyCodeFixAsync(testCase.TestCode, expected, testCase.FixedCode);
    }

    private static IEnumerable<object[]> GetInheritanceTestCases()
    {
        yield return [new DiagnosticTestCase(SingleInheritanceOnSameLine, SingleInheritanceOnSameLineFixed, "TestClass")];
        yield return [new DiagnosticTestCase(SingleInheritanceWithPrimaryConstructorOnSameLine, SingleInheritanceWithPrimaryConstructorOnSameLineFixed, "TestClass")];
        yield return [new DiagnosticTestCase(SingleInheritanceWithBadIndentation, SingleInheritanceWithBadIndentationExpectedFixed, "TestClass")];
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

    #region Trivia Preservation Test Case Data

    [StringSyntax("c#-test")]
    private const string MethodWithCommentsAndTwoParameters =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                // This method processes data
                public void ProcessData{|#0:(/* input */ int data, string format /* output format */)|}
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string MethodWithCommentsPreserved =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                // This method processes data
                public void ProcessData
                    (
                    /* input */ int data,
                    string format /* output format */
                    )
                {
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string InheritanceWithCommentsOnColonLine =
        """
        using System;
        using System.Threading.Tasks;

        namespace TestNamespace
        {
            public class SampleClient6 {|#0:: // niklas testar
                IAsyncDisposable
                // hello world
                , IDisposable|}
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => new ValueTask();
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string InheritanceWithCommentsOnColonPreserved =
        """
        using System;
        using System.Threading.Tasks;

        namespace TestNamespace
        {
            public class SampleClient6
                // niklas testar
                : IAsyncDisposable
                // hello world
                , IDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => new ValueTask();
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string InheritanceWithComments =
        """
        using System;
        
        namespace TestNamespace
        {
            // Important class comment
            public class TestClass /* inline comment */ {|#0:: IDisposable|}
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string InheritanceWithCommentsIntelligentlyRepositioned =
        """
        using System;
        
        namespace TestNamespace
        {
            // Important class comment
            public class TestClass /* inline comment */ 
                : IDisposable
            {
                public void Dispose() { }
            }
        }
        """;

    [StringSyntax("c#-test")]
    private const string InheritanceWithCommentsPreserved =
        """
        using System;
        
        namespace TestNamespace
        {
            // Important class comment
            public class TestClass /* inline comment */
                : IDisposable /* implements disposable */
            {
                public void Dispose() { }
            }
        }
        """;

    #endregion

    #region Trivia Preservation Tests

    [TestMethod]
    public async Task InheritanceWithCommentsOnColon_PreservesTrivia()
    {
        // Set up mock configuration to preserve trivia
        var mockConfig = new MockConfigurationService(
            triviaHandling: TriviaHandlingBehavior.Preserve,
            indentationUnit: "    ",
            lineEnding: "\r\n"
        );

        TestableNiklasifieraCodeFixProvider.MockConfigurationService = mockConfig;

        try
        {
            var test = new CSharpCodeFixTest<NiklasifieraAnalyzer, TestableNiklasifieraCodeFixProvider, DefaultVerifier>
            {
                TestCode = InheritanceWithCommentsOnColonLine.ReplaceLineEndings(),
                FixedCode = InheritanceWithCommentsOnColonPreserved.ReplaceLineEndings()
            };

            test.ExpectedDiagnostics
                .Add
                (
                    DiagnosticHelper
                        .Inheritance()
                        .WithLocation(0)
                        .WithArguments("SampleClient6")
                );

            await test.RunAsync();
        }
        finally
        {
            TestableNiklasifieraCodeFixProvider.MockConfigurationService = null;
        }
    }

    [TestMethod]
    public async Task InheritanceWithInlineComments_IntelligentlyRepositionsComments()
    {
        // Set up the mock configuration service
        var mockConfig = new MockConfigurationService(
            triviaHandling: TriviaHandlingBehavior.Preserve,
            indentationUnit: "    ",
            lineEnding: "\r\n"
        );

        TestableNiklasifieraCodeFixProvider.MockConfigurationService = mockConfig;

        try
        {
            var test = new CSharpCodeFixTest<NiklasifieraAnalyzer, TestableNiklasifieraCodeFixProvider, DefaultVerifier>
            {
                TestCode = InheritanceWithComments.ReplaceLineEndings(),
                FixedCode = InheritanceWithCommentsIntelligentlyRepositioned.ReplaceLineEndings()
            };

            test.ExpectedDiagnostics
                .Add
                (
                    DiagnosticHelper
                        .Inheritance()
                        .WithLocation(0)
                        .WithArguments("TestClass")
                );

            await test.RunAsync();
        }
        finally
        {
            TestableNiklasifieraCodeFixProvider.MockConfigurationService = null;
        }
    }

    #endregion

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
