namespace Niklasifiera.Samples;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

// Test primary constructor with multiple parameters on single line - should trigger analyzer
public class TestClient(HttpClient client, string apiKey, int timeout) : IDisposable
{
    public void DoStuff
        (
        )
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", apiKey);
        client.Timeout = TimeSpan.FromSeconds(timeout);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        client.Dispose();
    }
}

// Test record with multiple parameters on single line - should trigger analyzer  
public record PersonRecord(string FirstName, string LastName, int Age);

// Test method with generic parameters on single line - should trigger analyzer
public class TestService
{
    public static void ProcessData<T, U>(T data, U processor, string format)
        where T : class, new()
        where U : IProcessor<T>
    {
        // Processing logic
        Console.WriteLine(JsonSerializer.Serialize(new { data, processor, format }));
    }
}

public interface IProcessor<T> { }

public record TestClient2
(
    HttpClient client,
        string apiKey,
    int timeout
    );

// Test various parameter indentation issues
public class ParameterIndentTests
{
    // Wrong: opening paren not indented correctly
    public void Method1
  (
        int param1,
        string param2
    )
    {
    }
    
    // Wrong: parameters not aligned properly
    public void Method2
        (
    int param1,
            string param2,
        bool param3
        )
    {
    }
    
    // Wrong: closing paren not aligned with opening paren
    public void Method3
        (
        int param1,
        string param2
  )
    {
    }

    // Wrong: curly braces must be on their own lines aligned with the class indentation level
    public void Method4
        (
        int param1,
        string param2,
        bool param3
        ) {
    }

    // Correct: properly aligned parameters
    public void Method5
        (
        int param1,
        string param2,
        bool param3
        )
    {
    }
    
    // Wrong: closing brace not aligned with method declaration
    public void Method6
        (
        int param1,
        string param2
        )
    {
        // Some content
        }
    
    // Wrong: both opening and closing braces misaligned
    public void Method7
        (
        int param1,
        string param2
        )
        {
        // Some content
            }
}