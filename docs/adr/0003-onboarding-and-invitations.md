# 0003. Self-serve onboarding and email-bound invitations

Status: Accepted

## Context
Teams must be able to start using the product without manual setup by the
platform owner, and Admins must be able to add teammates safely.

## Decision
- A newly signed-in user can create an organization and becomes its Admin.
  The user, organization and membership are saved in one transaction. A user
  may administer at most 3 organizations, as a simple abuse limit.
- An invitation can only be accepted by the authenticated user whose email
  matches the invited address (compared case-insensitively). A mismatch looks
  like "not found". Invitations expire after 7 days.

## Alternatives considered
- Platform-owner-only provisioning: rejected, adds friction and a bottleneck.
- Accept-by-link: rejected, a leaked or forwarded link would give a stranger
  access to a customer's workspace.

## Consequences
- The email check is only as trustworthy as the email claim in the sign-in
  token, so the real Microsoft Entra configuration must use a verified claim.
- No emails are sent yet. The Admin shares the invitation id manually.

- Update after the first real Microsoft Entra run: the signed-in user is identified by the
  stable `oid` claim (with `sub` as a fallback). The email used for invitations comes from
  the `email` claim when present, otherwise from `preferred_username`. Microsoft documents
  `preferred_username` as changeable, so this is acceptable only while every user comes
  from our own tenant. Before real customers, switch to a verified email claim or to
  invitation links that carry a secret.