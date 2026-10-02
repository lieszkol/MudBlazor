namespace MudBlazor
{
    /// <summary>
    /// Fork addition. Optional service the host application registers (scoped, per circuit) to decide whether
    /// <see cref="MudInputControl.FieldDescription"/> is rendered at all. When it answers false the lightbulb and its
    /// <see cref="MudTooltip"/> are not generated, so the field costs nothing extra. KarbApp turns the descriptions off
    /// on a phone, where a hover tooltip does not work well.
    /// </summary>
    public interface IFieldDescriptionPolicy
    {
        /// <summary>False hides every field description. Read on every render of a control that has one.</summary>
        bool ShowFieldDescriptions { get; }
    }
}
