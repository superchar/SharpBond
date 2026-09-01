namespace SharpBond.Core.Tools;

[AttributeUsage(AttributeTargets.Method)]
public class ToolAttribute : Attribute
{
    public string Description { get; set; }
}