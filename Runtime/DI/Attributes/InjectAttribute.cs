using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Constructor | AttributeTargets.Method)]
public class InjectAttribute : Attribute
{
}
