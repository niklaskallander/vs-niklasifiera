namespace Niklasifiera.Samples;

using System.Net.Http;

public class SampleClient
    (
    HttpClient httpClient,
    Action<string> log
    )
    : IAsyncDisposable
    , IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        httpClient.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        httpClient.Dispose();

        return ValueTask.CompletedTask;
    }

    public async Task ExecuteAsync<T>
        (
        T request,
        CancellationToken cancellationToken = default
        )
        where T : HttpRequestMessage
    {
        log("Starting request...");

        using var response =
            await httpClient
                .SendAsync(request, cancellationToken);

        _ = response
            .EnsureSuccessStatusCode();

        var content =
            await response.Content
                .ReadAsStringAsync(cancellationToken);

        log($"Response received: {content}");
    }
}