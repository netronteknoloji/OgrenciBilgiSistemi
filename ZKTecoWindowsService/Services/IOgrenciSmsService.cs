// Services/IOgrenciSmsService.cs
using System.Threading;
using System.Threading.Tasks;

namespace ZKTecoWindowsService.Services
{
    public interface IOgrenciSmsService
    {
        Task<SmsSendResult> SendSmsAsync(string telefon, string mesaj, CancellationToken ct = default);
    }

    public sealed class SmsSendResult
    {
        public bool Success { get; init; }
        public string? ProviderMessageId { get; init; }
        public string RawResponse { get; init; } = "";
        public string? Error { get; init; }
        public int StatusCode { get; init; }
    }
}