namespace SharpBond.Core;

public record Unit : State
{
    public static readonly Unit Value = new();

    private Unit() : base(Guid.NewGuid())
    {
    }
}