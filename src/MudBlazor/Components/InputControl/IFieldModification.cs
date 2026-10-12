#nullable enable
using System.Threading.Tasks;

namespace MudBlazor
{
    /// <summary>
    /// Fork addition. What a form control hands to <see cref="MudInputControl.FieldModification"/> so that the control
    /// can show that its value differs from the one the form was opened with (a yellow field, like SAP Business One
    /// marks an unsaved change) and offer a small undo button next to it.
    /// The host application implements it (ZenUI: <c>ZenFieldModification&lt;T&gt;</c>), because only the host knows
    /// what "the value the form opened with" is; the library only asks the three questions below on every render.
    /// </summary>
    public interface IFieldModification
    {
        /// <summary>True while the field's current value differs from the original one. Read on every render of the control.</summary>
        bool IsModified { get; }

        /// <summary>
        /// The text of the undo button's native tooltip (it names the original value). Only read while
        /// <see cref="IsModified"/> is true.
        /// </summary>
        string? UndoTooltip { get; }

        /// <summary>
        /// Puts the original value back, through the control's own value setter so that the bound model and every
        /// change handler see it exactly like a value the user typed.
        /// </summary>
        Task UndoAsync();
    }
}
