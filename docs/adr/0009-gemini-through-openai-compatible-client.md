# 0009. Gemini through an OpenAI-compatible client

Status: Accepted

## Context
Ollama cannot run on a free web host, and the deployment must stay at no cost (ADR 0008).
Prompts can be large: transcripts are capped at 60,000 characters (about 15,000 tokens),
plus 1,500 tokens of output.

## Decision
- Use Google's free Gemini API (`gemini-3.8-flash`) through its OpenAI-compatible endpoint with
  Microsoft's `Microsoft.Extensions.AI.OpenAI` package. The provider, model, endpoint and key
  come from `Ai:*` settings (on Render: `Ai__Provider`, `Ai__Model`, `Ai__ApiKey`).
- The same client works with any OpenAI-compatible provider, so changing provider is a
  configuration change.
- A small wrapper in the host project translates the library's HTTP errors into the
  `HttpRequestException` the Strategy module already classifies, so the module stays
  provider-neutral and a rejected key is still a permanent configuration error.
- The key lives only in user-secrets locally and in the host's environment settings when deployed.

## Alternatives considered
- GitHub Models: input capped at 8,000 tokens per request, too small for long transcripts.
- Groq: roughly 8K tokens per minute on its main free models, too small for one long prompt.
- OpenRouter: 50 free requests per day, and the free model list rotates.
- Mistral free: prompts may be used for training.

## Consequences
- Google may use free-tier prompts to improve its products; only demo data is used.
- Free-tier limits are not published per model and can change; the model name is configuration.
- Google labels the OpenAI-compatibility layer beta. A community Gemini package is the fallback.
- Gemini models spend output tokens on thinking; if drafts are cut off, raise the output cap.
An optional second model (Ai:FallbackModel) is tried when the first returns a server error, a quota error or no answer; configuration errors are never retried.