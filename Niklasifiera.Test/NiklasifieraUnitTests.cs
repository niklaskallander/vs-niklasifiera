namespace Niklasifiera.Test;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Diagnostics.CodeAnalysis;

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

    #endregion

    // Helper method to get the signature diagnostic descriptor
    private static DiagnosticResult SignatureDiagnostic()
    {
        var analyzer =
            new NiklasifieraAnalyzer();

        var rule =
            analyzer
                .SupportedDiagnostics[0]; // First diagnostic is the signature rule

        return VerifyCS.Diagnostic(rule);
    }

    // Helper method to get the inheritance diagnostic descriptor  
    private static DiagnosticResult InheritanceDiagnostic()
    {
        var analyzer =
            new NiklasifieraAnalyzer();

        var rule =
            analyzer
                .SupportedDiagnostics[1]; // Second diagnostic is the inheritance rule

        return VerifyCS
            .Diagnostic(rule);
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
    public async Task NoDiagnostic_Tests
        (
        [StringSyntax("c#-test")] string testCode
        )
    {
        await VerifyCS
            .VerifyAnalyzerAsync(testCode);
    }

    // Tests for signature diagnostics (methods/constructors with multiple parameters on single line)
    [TestMethod]
    [DataRow(MethodWithTwoParametersOnSingleLine, MethodWithTwoParametersOnSingleLineFixed, "TestMethod", DisplayName = "Method with two parameters on single line")]
    [DataRow(ConstructorWithTwoParametersOnSingleLine, ConstructorWithTwoParametersOnSingleLineFixed, "TestClass", DisplayName = "Constructor with two parameters on single line")]
    [DataRow(MethodWithThreeParametersOnSingleLine, MethodWithThreeParametersOnSingleLineFixed, "TestMethod", DisplayName = "Method with three parameters on single line")]
    [DataRow(GenericMethodWithMultipleParametersOnSingleLine, GenericMethodWithMultipleParametersOnSingleLineFixed, "ExecuteAsync", DisplayName = "Generic method with multiple parameters on single line")]
    [DataRow(MethodWithGenericConstraintsOnSingleLine, MethodWithGenericConstraintsOnSingleLineFixed, "ProcessData", DisplayName = "Method with generic constraints on single line")]
    [DataRow(RecordWithTwoParametersOnSingleLine, RecordWithTwoParametersOnSingleLineFixed, "Person", DisplayName = "Record with two parameters on single line")]
    public async Task SignatureDiagnostic_Tests
        (
        [StringSyntax("c#-test")] string testCode,
        [StringSyntax("c#-test")] string fixedCode,
        string expectedMethodName
        )
    {
        var expected =
            SignatureDiagnostic()
                .WithLocation(0)
                .WithArguments(expectedMethodName);

        await VerifyCS
            .VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    // Special case for primary constructor with inheritance (different location)
    [TestMethod]
    public async Task PrimaryConstructorWithInheritanceOnSingleLine_Diagnostic()
    {
        var expected =
            SignatureDiagnostic()
                .WithLocation(7, 30)
                .WithArguments("SampleClient");

        await VerifyCS
            .VerifyCodeFixAsync(PrimaryConstructorWithInheritanceOnSingleLine, expected, PrimaryConstructorWithInheritanceOnSingleLineFixed);
    }

    // Tests for inheritance diagnostics
    [TestMethod]
    [DataRow(SingleInheritanceOnSameLine, SingleInheritanceOnSameLineFixed, "TestClass", DisplayName = "Single inheritance on same line")]
    [DataRow(SingleInheritanceWithPrimaryConstructorOnSameLine, SingleInheritanceWithPrimaryConstructorOnSameLineFixed, "TestClass", DisplayName = "Single inheritance with primary constructor on same line")]
    public async Task InheritanceDiagnostic_Tests
        (
        [StringSyntax("c#-test")] string testCode,
        [StringSyntax("c#-test")] string fixedCode,
        string expectedClassName
        )
    {
        var expected =
            InheritanceDiagnostic()
                .WithLocation(0)
                .WithArguments(expectedClassName);

        await VerifyCS
            .VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
