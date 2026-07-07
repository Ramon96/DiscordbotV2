/// <summary>
/// Marks an <see cref="IDiscordCommand"/> as deprecated so auto-registration skips it: the command
/// is not added to DI, published as a slash command, or dispatched. The class is kept intact so it
/// can be revived later simply by removing this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DeprecatedCommandAttribute : Attribute
{
}
