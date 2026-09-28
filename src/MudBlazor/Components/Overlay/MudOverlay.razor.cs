using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.State;
using MudBlazor.Utilities;

namespace MudBlazor
{
#nullable enable
    public partial class MudOverlay : MudComponentBase, IAsyncDisposable
    {
        private IParameterState<bool> _visibleState;

        protected string Classname =>
            new CssBuilder("mud-overlay")
                .AddClass("mud-overlay-absolute", Absolute)
                .AddClass(Class)
                .Build();

        protected string ScrimClassname =>
            new CssBuilder("mud-overlay-scrim")
                .AddClass("mud-overlay-dark", DarkBackground)
                .AddClass("mud-overlay-light", LightBackground)
                .Build();

        protected string Styles =>
            new StyleBuilder()
                .AddStyle("z-index", $"{ZIndex}", ZIndex != 5)
                .AddStyle(Style)
                .Build();

        [Inject]
        public IScrollManager ScrollManager { get; set; } = null!;

        /// <summary>
        /// Child content of the component.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Fires when Visible changes
        /// </summary>
        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        /// <summary>
        /// If true overlay will be visible. Two-way bindable.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public bool Visible { get; set; }

        /// <summary>
        /// If true overlay will set Visible false on click.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.ClickAction)]
        public bool AutoClose { get; set; }

        /// <summary>
        /// If true (default), the Document.body element will not be able to scroll
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public bool LockScroll { get; set; } = true;

        /// <summary>
        /// The css class that will be added to body if lockscroll is used.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public string LockScrollClass { get; set; } = "scroll-locked";

        /// <summary>
        /// If true applies the themes dark overlay color.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public bool DarkBackground { get; set; }

        /// <summary>
        /// If true applies the themes light overlay color.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public bool LightBackground { get; set; }

        /// <summary>
        /// If true, use absolute positioning for the overlay.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public bool Absolute { get; set; }

        /// <summary>
        /// Sets the z-index of the overlay.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.Behavior)]
        public int ZIndex { get; set; } = 5;

        /// <summary>
        /// Command parameter.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.ClickAction)]
        [Obsolete($"This will be removed in v7.")]
        public object? CommandParameter { get; set; }

        /// <summary>
        /// Command executed when the user clicks on an element.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Overlay.ClickAction)]
        [Obsolete($"Use {nameof(OnClick)} instead. This will be removed in v7.")]
        public ICommand? Command { get; set; }

        /// <summary>
        /// Fired when the overlay is clicked
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        public MudOverlay()
        {
            _visibleState = RegisterParameter(nameof(Visible), () => Visible, () => VisibleChanged, VisibleParameterChangedHandlerAsync);
        }

        protected internal async Task OnClickHandlerAsync(MouseEventArgs ev)
        {
            if (AutoClose)
            {
                await _visibleState.SetValueAsync(false);
            }

            await OnClick.InvokeAsync(ev);
#pragma warning disable CS0618
            if (Command?.CanExecute(CommandParameter) ?? false)
            {
                Command.Execute(CommandParameter);
            }
#pragma warning restore CS0618
        }

        // KarbApp fork: true while THIS overlay holds the body scroll lock. Stock MudBlazor called Lock/UnlockScrollAsync
        // after EVERY render and again on dispose, whether or not the overlay was ever shown. Each call is a JS interop
        // round trip whose unlockScroll removes two classes from <body> even when they are absent, which still rewrites
        // the class attribute. A table page keeps one closed overlay per ZenTh column header, so an issue-list load sent
        // 168 no-op unlocks and a navigation away 100 (measured 2026-09-28). Every rewrite also woke body-level
        // MutationObservers (the KeePassXC-Browser extension rescanned every input with hit tests and pinned Edge at 100%
        // CPU). A hidden overlay's re-render could also remove the lock of another overlay that was still open.
        private bool _scrollLockHeld;

        //if not visible or CSS `position:absolute`, don't lock scroll
        protected override async Task OnAfterRenderAsync(bool firstTime)
        {
            // _visibleState.Value is what the markup shows: AutoClose hides the overlay before the parent's Visible follows.
            if (LockScroll && !Absolute && _visibleState.Value)
            {
                // Re-asserted on every render while shown, as before: another overlay closing in the meantime removes
                // the shared body class.
                _scrollLockHeld = true;
                await BlockScrollAsync();
            }
            else if (_scrollLockHeld)
            {
                _scrollLockHeld = false;
                await UnblockScrollAsync();
            }
        }

        private Task VisibleParameterChangedHandlerAsync()
        {
            return VisibleChanged.InvokeAsync(_visibleState.Value);
        }

        //locks the scroll attaching a CSS class to the specified element, in this case the body
        private ValueTask BlockScrollAsync()
        {
            return ScrollManager.LockScrollAsync("body", LockScrollClass);
        }

        //removes the CSS class that prevented scrolling
        private ValueTask UnblockScrollAsync()
        {
            return ScrollManager.UnlockScrollAsync("body", LockScrollClass);
        }

        //When disposing the overlay, remove the class that prevented scrolling
        public ValueTask DisposeAsync()
        {
            // Only an overlay that locked releases the lock (see _scrollLockHeld).
            if (_scrollLockHeld && IsJSRuntimeAvailable)
            {
                _scrollLockHeld = false;
                return UnblockScrollAsync();
            }

            return ValueTask.CompletedTask;
        }
    }
}
