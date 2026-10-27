namespace fasterLegit.Domain.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CustomizeCategoryAttribute : Attribute
{
    public Type? PageType { get; init; }
}
