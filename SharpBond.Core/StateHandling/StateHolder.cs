namespace SharpBond.Core.StateHandling;

public static class StateHolder
{
    public static AsyncLocal<State> State { get; set; } = new();
}