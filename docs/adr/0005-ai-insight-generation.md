# 0005. AI insight generation

Status: Proposed

## Context
CreatorAnalytics must turn a video's transcript and audience-retention data into a draft
strategy (pacing fixes, thumbnail concepts). AI output is unreliable and costs money, and
transcripts are untrusted input.

## Decision (proposed)
- The generator lives in the Strategy module behind `IInsightGenerator`. The module that
  owns revisions owns generation. Inputs are gathered through the Integration contract.
- The system computes retention facts itself (largest drop-offs with timestamps). The
  generator receives those facts and the transcript, so numbers are never invented.
- Generation is asynchronous: an endpoint records a generation request and returns 202. A
  background worker, working one tenant at a time, produces the draft and stores it as a
  revision with origin AI and no author. Allowed only while the strategy is Draft or
  Needs Revision.
- Output is untrusted text: stored as a revision, never executed, always human-reviewed
  before use. The model gets no tools. Transcripts are length-limited.
- Rejection reasons from earlier reviews of the same video are added to the next prompt as
  constraints. This is context for one request, not model training.
- Each request records the model, prompt version and token counts. Requests are capped per
  strategy and per organization per day. Failures are retried a few times, then reported;
  a failure never changes a strategy.
- A deterministic template generator comes first; the real LLM implementation is added
  later behind the same interface.

## Consequences
- The whole flow can be built and tested without an AI provider.
- A human always stands between AI output and any use of it.
- Drafts are not instant; clients must check the request's status.

## Open questions
- Which AI provider and model for the real generator.
- Decided: at most 5 AI drafts per strategy, one open request at a time (both enforced by
  the strategy itself and, for the open request, by the database). A per-organization daily
  cap is still open.
- Whether a later extraction into a Python service changes the worker's home.