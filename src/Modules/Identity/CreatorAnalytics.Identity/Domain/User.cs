using System;

namespace CreatorAnalytics.Identity.Domain;

public class User
{
    public Guid Id { get; private set; }
    // This will hold the unique ID given to us by Microsoft Entra ID
    public string ExternalId { get; private set; }
    public string Email { get; private set; }

    public User(string externalId, string email)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External identity provider ID is required.", nameof(externalId));

        Id = Guid.NewGuid();
        ExternalId = externalId;
        Email = email;
    }
}