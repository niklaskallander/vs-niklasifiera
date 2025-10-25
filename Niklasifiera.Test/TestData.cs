namespace Niklasifiera.Test;

using System.Diagnostics.CodeAnalysis;

internal static class TestData
{
    #region Test Case Data - No Diagnostics

    [StringSyntax("c#-test")]
    public const string EmptyCode = "";

    [StringSyntax("c#-test")]
    public const string MethodWithNoParameters =
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
    public const string MethodWithOneParameter =
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
    public const string MethodWithMultipleParametersCorrectlyFormatted =
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
    public const string ConstructorWithNoParameters =
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
    public const string ConstructorWithOneParameter =
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
    public const string GenericMethodWithOneParameter =
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
    public const string PrimaryConstructorWithInheritanceCorrectlyFormatted =
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
    public const string SingleInheritanceCorrectlyFormatted =
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
    public const string SingleInheritanceWithPrimaryConstructorCorrectlyFormatted =
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
    public const string RecordWithTwoParametersCorrectlyFormatted =
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
    public const string RecordWithOneParameter =
        """
        namespace TestNamespace
        {
            public record Person(string Name);
        }
        """;

    #endregion

    #region Test Case Data - Signature Diagnostics

    [StringSyntax("c#-test")]
    public const string MethodWithTwoParametersOnSingleLine =
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
    public const string MethodWithTwoParametersOnSingleLineFixed =
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
    public const string ConstructorWithTwoParametersOnSingleLine =
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
    public const string ConstructorWithTwoParametersOnSingleLineFixed =
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
    public const string MethodWithThreeParametersOnSingleLine =
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
    public const string MethodWithThreeParametersOnSingleLineFixed =
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
    public const string GenericMethodWithMultipleParametersOnSingleLine =
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
    public const string GenericMethodWithMultipleParametersOnSingleLineFixed =
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
    public const string PrimaryConstructorWithInheritanceOnSingleLine =
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
    public const string PrimaryConstructorWithInheritanceOnSingleLineFixed =
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
    public const string MethodWithGenericConstraintsOnSingleLine =
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
    public const string MethodWithGenericConstraintsOnSingleLineFixed =
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
    public const string RecordWithTwoParametersOnSingleLine =
        """
        namespace TestNamespace
        {
            public record Person{|#0:(string FirstName, string LastName)|};
        }
        """;

    [StringSyntax("c#-test")]
    public const string RecordWithTwoParametersOnSingleLineFixed =
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
    public const string SingleInheritanceOnSameLine =
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
    public const string SingleInheritanceOnSameLineFixed =
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
    public const string SingleInheritanceWithPrimaryConstructorOnSameLine =
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
    public const string SingleInheritanceWithPrimaryConstructorOnSameLineFixed =
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
    public const string SingleInheritanceWithBadIndentation =
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
    public const string SingleInheritanceWithBadIndentationExpectedFixed =
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
    public const string ConditionalOperatorSingleLine =
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
    public const string ConditionalOperatorSingleLineFixed =
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
    public const string ConditionalOperatorAssignmentSingleLine =
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
    public const string ConditionalOperatorAssignmentSingleLineFixed =
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
    public const string ConditionalOperatorPartialSplit =
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
    public const string ConditionalOperatorPartialSplitFixed =
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
    public const string ConditionalOperatorWrongIndentation =
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
    public const string ConditionalOperatorWrongIndentationFixed =
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
    public const string ConditionalOperatorReturnCorrectlyFormatted =
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
    public const string ConditionalOperatorAssignmentCorrectlyFormatted =
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

    #region Test Case Data - Nested Conditional Operator Diagnostics

    [StringSyntax("c#-test")]
    public const string NestedConditionalSingleLine =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    return {|#0:condition1 ? (condition2 ? "A" : "B") : "C"|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalSingleLineFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    return condition1
                        ? (condition2
                            ? "A"
                            : "B")
                        : "C";
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalInAssignment =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    var result = {|#0:condition1 ? (condition2 ? "A" : "B") : "C"|};
                    return result;
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalInAssignmentFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    var result =
                        condition1
                            ? (condition2
                                ? "A"
                                : "B")
                            : "C";
                    return result;
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string DeeplyNestedConditional =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var c1 = true;
                    var c2 = false;
                    var c3 = true;
                    return {|#0:c1 ? (c2 ? (c3 ? "A" : "B") : "C") : "D"|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string DeeplyNestedConditionalFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var c1 = true;
                    var c2 = false;
                    var c3 = true;
                    return c1
                        ? (c2
                            ? (c3
                                ? "A"
                                : "B")
                            : "C")
                        : "D";
                }
            }
        }
        """;    [StringSyntax("c#-test")]
    public const string NestedConditionalInFalseBranch =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    return {|#0:condition1 ? "A" : (condition2 ? "B" : "C")|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalInFalseBranchFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    return condition1
                        ? "A"
                        : (condition2
                            ? "B"
                            : "C");
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalPartiallyFormatted =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    return {|#0:condition1
                        ? (condition2 ? "A" : "B")
                        : "C"|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalPartiallyFormattedFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    return condition1
                        ? (condition2
                            ? "A"
                            : "B")
                        : "C";
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalWithComplexCondition =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return {|#0:condition.ToString() == "True" ? (condition.GetHashCode() == 1 ? "A" : "B") : "C"|};
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalWithComplexConditionFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition = true;
                    return condition.ToString() == "True"
                        ? (condition.GetHashCode() == 1
                            ? "A"
                            : "B")
                        : "C";
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalCorrectlyFormatted =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    return condition1
                        ? (condition2
                            ? "A"
                            : "B")
                        : "C";
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalInAssignmentCorrectlyFormatted =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string TestMethod()
                {
                    var condition1 = true;
                    var condition2 = false;
                    var result =
                        condition1
                            ? (condition2
                                ? "A"
                                : "B")
                            : "C";
                    return result;
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalWithoutParensMultiLineCondition =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string Assignment()
                {
                    var condition = true;
                    var something =
                        {|#0:condition
                            .ToString() == "True" ? condition
                                    .GetHashCode() == 3 ? "Yes"
                                    : "No"
                                : "No"|};
                    return something;
                }
            }
        }
        """;

    [StringSyntax("c#-test")]
    public const string NestedConditionalWithoutParensMultiLineConditionFixed =
        """
        namespace TestNamespace
        {
            public class TestClass
            {
                public string Assignment()
                {
                    var condition = true;
                    var something =
                        condition
                            .ToString() == "True"
                                ? condition
                                    .GetHashCode() == 3
                                        ? "Yes"
                                        : "No"
                                : "No";
                    return something;
                }
            }
        }
        """;

    #endregion

    #region Trivia Preservation Test Case Data

    [StringSyntax("c#-test")]
    public const string MethodWithCommentsAndTwoParameters =
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
    public const string MethodWithCommentsPreserved =
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
    public const string InheritanceWithCommentsOnColonLine =
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
    public const string InheritanceWithCommentsOnColonPreserved =
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
    public const string InheritanceWithComments =
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
    public const string InheritanceWithCommentsIntelligentlyRepositioned =
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
    public const string InheritanceWithCommentsPreserved =
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

}

