using MongoDB.Bson;

namespace Reviews.Domain.Common;

/// <summary>База для документних сутностей MongoDB: ObjectId, службові дати, версія для оптимістичної конкурентності.</summary>
public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public int Version { get; protected set; }

    protected void Touch() => UpdatedAt = DateTime.UtcNow;
    public void BumpVersion() => Version++;
}
