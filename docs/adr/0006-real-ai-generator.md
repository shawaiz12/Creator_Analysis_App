# 0006. The real AI generator

Status: Proposed

## Context
Draft generation works end to end with a deterministic template writer. A real language
model should write richer drafts without weakening any safety property from ADR 0005.

## Decision (proposed)
- One implementation of `IInsightGenerator` calls a language model through the
  provider-neutral `IChatClient` abstraction (Microsoft.Extensions.AI), so the provider is
  chosen by configuration.
- Provider, model name and API key come from configuration (the key from user-secrets
  locally and a secret store when deployed), never from source control. Model names are never
  hard-coded because providers rename and retire models. With no key configured the
  application keeps using the template generator.
- The model sees only: video title, duration, transcript (capped), the computed drop-offs and
  earlier rejection reasons. It never sees organization ids, user ids, emails or any other
  identifier.
- The prompt is a fixed system prompt that says everything inside tags is data and never
  instructions, and forbids inventing numbers. Untrusted text is placed inside tags and cannot
  contain `<` or `>`, so it cannot close a tag. Output is capped at 1,500 tokens.
- Output is validated before use: non-empty, length-limited, required sections present,
  control characters removed. A draft that fails validation is a transient failure and follows
  the retry rules.
- Timeouts, rate limits and server errors are transient. Bad requests are permanent. Rejected
  credentials are permanent and logged as critical because they are a configuration problem.
- Whatever the model writes becomes an AI revision that a human must review. The model has no
  tools and cannot change anything.
- Automated tests never call a real provider. A fake chat client covers success, each error
  class and bad output. A check against the real provider is run by hand.

## Consequences
- Provider-neutral code and a safe fallback, at the cost of one extra abstraction.
- Real usage costs money. Output size, the five-drafts-per-strategy cap and a spending limit
  set in the provider's console bound it.
- The "no identifiers are sent" rule must be repeated in the product's terms later.

## Open questions
- Which provider and model.
- Whether to add a per-organization daily cap and token accounting on each request.