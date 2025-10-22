namespace Niklasifiera.Samples;

using System;
using System.Data;

// This should trigger the inheritance analyzer
public class TestClass : IDisposable, IComparable<int>
{
    public void Dispose() { }
    public int CompareTo(int other) => 0;
}

// This case from IMPLEMENTATION_SUMMARY.md should also trigger analyzer
public class SampleClient6 : // niklas testar
    IAsyncDisposable
    // hello world
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// EXPECTED output from code-fix (when preserving trivia):
public class SampleClient6Expected
    // niklas testar
    : IAsyncDisposable
    // hello world
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// ACTUAL output from code-fix (when preserving trivia):
public class SampleClient6Actual
    : IAsyncDisposable
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Wrong spacing after colon (no space)
public class WrongSpacing1
    : IAsyncDisposable
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Wrong spacing after colon (multiple spaces)
public class WrongSpacing2
    : IAsyncDisposable
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Wrong spacing after comma (no space)
public class WrongSpacing3
    : IAsyncDisposable
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Wrong spacing after comma (multiple spaces)
public class WrongSpacing4
    : IAsyncDisposable
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Wrong indentation - colon too far indented
public class WrongIndent1
        : IAsyncDisposable
        , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Wrong indentation - colon not indented enough
public class WrongIndent2
: IAsyncDisposable
, IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Wrong indentation - comma not aligned with colon
public class WrongIndent3
    : IAsyncDisposable
      , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Correctly formatted (what it should look like)
public class CorrectFormat
    : IAsyncDisposable
    , IDisposable
{
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Test case for single inheritance
public class TestClass1 : IDisposable
{
    public void Dispose() { }
}

// This should be formatted as:
public class TestClass2
    : IDisposable
{
    public void Dispose() { }
}

// Test case for single inheritance with primary constructor
public class TestClass3
    (
    IDbConnection DbConnection,
    IDbTransaction DbTransaction
    ) : IDisposable
{
    public void Dispose() { }
}

// This should be formatted as:
public class TestClass4
    (
    IDbConnection DbConnection,
    IDbTransaction DbTransaction
    )
    : IDisposable
{
    public void Dispose() { }
}

// Test case for single inheritance with bad indentation
public class TestClass5
     : IDisposable
{
    public void Dispose() { }
}

// This should be formatted as:
public class TestClass6
    : IDisposable
{
    public void Dispose() { }
}
