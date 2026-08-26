namespace SharpBond.Core.Abstractions;

public interface ILlm
{
    Task<string> GenerateAsync(string prompt);
    
    IAsyncEnumerable<string> GenerateStreamingAsync(string prompt, CancellationToken cancellationToken);
}