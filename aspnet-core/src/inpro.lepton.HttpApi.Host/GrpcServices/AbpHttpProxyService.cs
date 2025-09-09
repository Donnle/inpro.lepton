using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Grpc.Core;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Inpro.Lepton.Grpc;

namespace Inpro.Lepton.Grpc
{
    public class AbpHttpProxyService : AbpHttpProxy.AbpHttpProxyBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AbpHttpProxyService> _logger;

        public AbpHttpProxyService(IHttpClientFactory httpClientFactory,
                                   ILogger<AbpHttpProxyService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public override async Task<HttpCallResponse> Call(HttpCallRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return new HttpCallResponse
                {
                    StatusCode = 400,
                    Body = ByteString.CopyFromUtf8("Path is required.")
                };
            }

            var method = new HttpMethod(
                string.IsNullOrWhiteSpace(request.Method) ? "GET" : request.Method.ToUpperInvariant());

            // Дозволяємо і абсолютні, і відносні URL
            HttpRequestMessage msg = Uri.TryCreate(request.Path, UriKind.Absolute, out var abs)
                ? new HttpRequestMessage(method, abs)
                : new HttpRequestMessage(method, request.Path.StartsWith("/") ? request.Path : "/" + request.Path);

            // Якщо є тіло — кладемо байти як є, БЕЗ дефолтного Content-Type
            if (request.Body != null && request.Body.Length > 0 &&
                (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
            {
                msg.Content = new ByteArrayContent(request.Body.ToByteArray());
            }

            // Проксіюємо заголовки (нічого не нормалізуємо)
            foreach (var h in request.Headers)
            {
                if (string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(h.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!msg.Headers.TryAddWithoutValidation(h.Key, h.Value))
                    msg.Content?.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            // Якщо в gRPC-метаданих прилетів Authorization — теж прокинемо
            var metaAuth = context.RequestHeaders?.GetValue("authorization");
            if (!string.IsNullOrEmpty(metaAuth) && !msg.Headers.Contains("Authorization"))
            {
                msg.Headers.TryAddWithoutValidation("Authorization", metaAuth);
            }

            var client = _httpClientFactory.CreateClient("AbpSelf");

            try
            {
                using var resp = await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, context.CancellationToken);
                var bytes = await resp.Content.ReadAsByteArrayAsync(context.CancellationToken);

                var response = new HttpCallResponse
                {
                    StatusCode = (int)resp.StatusCode,
                    Body = ByteString.CopyFrom(bytes) // <-- «сирі» байти; Postman показує як base64
                };

                foreach (var h in resp.Headers)
                    foreach (var v in h.Value)
                        response.Headers.Add(new Header { Key = h.Key, Value = v });

                foreach (var h in resp.Content.Headers)
                    foreach (var v in h.Value)
                        response.Headers.Add(new Header { Key = h.Key, Value = v });

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while calling {Path}", request.Path);
                return new HttpCallResponse
                {
                    StatusCode = 502,
                    Body = ByteString.CopyFromUtf8($"Upstream error: {ex.Message}")
                };
            }
            catch (TaskCanceledException)
            {
                return new HttpCallResponse
                {
                    StatusCode = 504,
                    Body = ByteString.CopyFromUtf8("Upstream timeout.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while calling {Path}", request.Path);
                return new HttpCallResponse
                {
                    StatusCode = 502,
                    Body = ByteString.CopyFromUtf8($"Upstream error: {ex.Message}")
                };
            }
        }
    }

    internal static class GrpcMetadataExtensions
    {
        public static string? GetValue(this Metadata? metadata, string key)
        {
            if (metadata == null) return null;
            foreach (var e in metadata)
                if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))
                    return e.Value;
            return null;
        }
    }
}
