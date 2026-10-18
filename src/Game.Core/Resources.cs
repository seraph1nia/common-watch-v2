namespace Game.Core;

public enum Resource { Gold, Wood, Food, Stone, Metal, Cloth }

// Costs, balances and receipts share bounded whole-resource arithmetic. A failed
// operation produces no prospective balance, so callers cannot commit a prefix.
public readonly record struct ResourceCost(int Gold = 0, int Wood = 0, int Food = 0, int Stone = 0, int Metal = 0, int Cloth = 0)
{
    public bool IsValid => Gold >= 0 && Wood >= 0 && Food >= 0 && Stone >= 0 && Metal >= 0 && Cloth >= 0;
    public int Amount(Resource resource) => resource switch
    {
        Resource.Gold => Gold,
        Resource.Wood => Wood,
        Resource.Food => Food,
        Resource.Stone => Stone,
        Resource.Metal => Metal,
        Resource.Cloth => Cloth,
        _ => throw new ArgumentOutOfRangeException(nameof(resource))
    };
    public ResourceCost With(Resource resource, int amount) => resource switch
    {
        Resource.Gold => this with { Gold = amount },
        Resource.Wood => this with { Wood = amount },
        Resource.Food => this with { Food = amount },
        Resource.Stone => this with { Stone = amount },
        Resource.Metal => this with { Metal = amount },
        Resource.Cloth => this with { Cloth = amount },
        _ => throw new ArgumentOutOfRangeException(nameof(resource))
    };
    public bool CanPay(int gold, int wood, int food, int stone = 0, int metal = 0, int cloth = 0)
        => new ResourceCost(gold, wood, food, stone, metal, cloth).TryPay(this, out _);
    public bool TryAdd(ResourceCost income, out ResourceCost result)
    {
        ResourceCost balance = this;
        result = default;
        return balance.IsValid && income.IsValid && TryCreate((long)balance.Gold + income.Gold, (long)balance.Wood + income.Wood, (long)balance.Food + income.Food,
            (long)balance.Stone + income.Stone, (long)balance.Metal + income.Metal, (long)balance.Cloth + income.Cloth, out result);
    }
    public bool TryPay(ResourceCost cost, out ResourceCost result)
    {
        ResourceCost balance = this;
        result = default;
        return balance.IsValid && cost.IsValid && TryCreate((long)balance.Gold - cost.Gold, (long)balance.Wood - cost.Wood, (long)balance.Food - cost.Food,
            (long)balance.Stone - cost.Stone, (long)balance.Metal - cost.Metal, (long)balance.Cloth - cost.Cloth, out result);
    }
    public bool TryMultiply(int quantity, out ResourceCost result)
    {
        ResourceCost balance = this;
        result = default;
        return balance.IsValid && quantity >= 0 && TryCreate((long)balance.Gold * quantity, (long)balance.Wood * quantity, (long)balance.Food * quantity,
            (long)balance.Stone * quantity, (long)balance.Metal * quantity, (long)balance.Cloth * quantity, out result);
    }
    public ResourceCost HalfRefund() => IsValid ? new(Gold / 2, Wood / 2, Food / 2, Stone / 2, Metal / 2, Cloth / 2)
        : throw new InvalidOperationException("Invalid resource investment.");
    private static bool TryCreate(long gold, long wood, long food, long stone, long metal, long cloth, out ResourceCost result)
    {
        result = default;
        if (gold is < 0 or > int.MaxValue || wood is < 0 or > int.MaxValue || food is < 0 or > int.MaxValue
            || stone is < 0 or > int.MaxValue || metal is < 0 or > int.MaxValue || cloth is < 0 or > int.MaxValue) return false;
        result = new((int)gold, (int)wood, (int)food, (int)stone, (int)metal, (int)cloth); return true;
    }
}
