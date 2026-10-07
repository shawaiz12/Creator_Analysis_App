using System;

namespace CreatorAnalytics.Identity.Domain;

public class Organization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public Organization(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
    }
}