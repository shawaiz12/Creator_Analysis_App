using System.ClientModel;
using System.Net;
using Microsoft.Extensions.AI;

namespace CreatorAnalytics.Api.Ai;

/// <summary>
/// The OpenAI library reports HTTP failures as ClientResultException. The Strategy module only
/// knows HttpRequestException and nothing about any provider library, so this wrapper
/// translates one into the other at the edge of the application.
/// </summary>
internal sealed class StatusMappingChatClient : DelegatingChatClient
{
    public StatusMappingChatClient(IChatClient inner) : base(inner)
    {
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (ClientResultException ex)
        {
            HttpStatusCode? status = ex.Status > 0 ? (HttpStatusCode)ex.Status : null;
            throw new HttpRequestException(ex.Message, ex, status);
        }
    }
}