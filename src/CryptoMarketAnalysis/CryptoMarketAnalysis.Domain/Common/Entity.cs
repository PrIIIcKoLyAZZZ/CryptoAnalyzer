namespace CryptoMarketAnalysis.Domain.Common;

public abstract class Entity
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(id)} cannot be empty");
        }

        Id = id;
    }

    protected Entity()
    {
    }

    public Guid Id { get; protected set; }
}