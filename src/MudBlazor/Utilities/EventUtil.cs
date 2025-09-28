using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace MudBlazor
{
#nullable enable
    /// <summary>
    /// Utility class for opting out of rerendering in Blazor when an EventCallback is invoked.
    /// By default, components inherit from ComponentBase, which automatically invokes StateHasChanged
    /// after the component's event handlers are invoked. In some cases, it might be unnecessary or
    /// undesirable to trigger a rerender after an event handler is invoked. For example, an event
    /// handler might not modify the component state.
    /// https://learn.microsoft.com/en-us/aspnet/core/blazor/performance?view=aspnetcore-6.0#avoid-rerendering-after-handling-events-without-state-changes
    /// </summary>
    public static class EventUtil
    {
        #region FromMudBlazorV8
        /// <summary>
        /// Converts the provided <see cref="Action"/> callback into a non-rendering event handler.
        /// </summary>
        /// <param name="component">The component that handles exceptions.</param>
        /// <param name="callback">The action callback to be converted.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Action AsNonRenderingEventHandler(this ComponentBase component, Action callback)
            => new SyncReceiver(callback).Invoke;

        /// <summary>
        /// Converts the provided <see cref="Action{TValue}"/> callback into a non-rendering event handler.
        /// </summary>
        /// <typeparam name="TValue">The type of the callback argument.</typeparam>
        /// <param name="callback">The action callback to be converted.</param>
        /// <param name="component">The component that handles exceptions.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Action<TValue> AsNonRenderingEventHandler<TValue>(this ComponentBase component, Action<TValue> callback)
            => new SyncReceiver<TValue>(callback).Invoke;

        /// <summary>
        /// Converts the provided <see cref="Func{Task}"/> callback into a non-rendering event handler.
        /// </summary>
        /// <param name="callback">The asynchronous callback to be converted.</param>
        /// <param name="component">The component that handles exceptions.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Func<Task> AsNonRenderingEventHandler(this ComponentBase component, Func<Task> callback)
            => new AsyncReceiver(callback).Invoke;

        /// <summary>
        /// Converts the provided <see cref="Func{TValue, Task}"/> callback into a non-rendering event handler.
        /// </summary>
        /// <typeparam name="TValue">The type of the callback argument.</typeparam>
        /// <param name="callback">The asynchronous callback to be converted.</param>
        /// <param name="component">The component that handles exceptions.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Func<TValue, Task> AsNonRenderingEventHandler<TValue>(this ComponentBase component, Func<TValue, Task> callback)
            => new AsyncReceiver<TValue>(callback).Invoke;

        //private record SyncReceiver(ComponentBase component, Action callback) : ReceiverBase()
        //{
        //    public void Invoke() => callback();
        //}

        //private record SyncReceiver<T>(ComponentBase component, Action<T> callback) : ReceiverBase()
        //{
        //    public void Invoke(T arg) => callback(arg);
        //}

        //private record AsyncReceiver(ComponentBase component, Func<Task> callback) : ReceiverBase()
        //{
        //    public Task Invoke() => callback();
        //}

        //private record AsyncReceiver<T>(ComponentBase component, Func<T, Task> callback) : ReceiverBase()
        //{
        //    public Task Invoke(T arg) => callback(arg);
        //}
        #endregion

        /// <summary>
        /// Converts the provided <see cref="Action"/> callback into a non-rendering event handler.
        /// </summary>
        /// <param name="callback">The action callback to be converted.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Action AsNonRenderingEventHandler(Action callback)
            => new SyncReceiver(callback).Invoke;

        /// <summary>
        /// Converts the provided <see cref="Action{TValue}"/> callback into a non-rendering event handler.
        /// </summary>
        /// <typeparam name="TValue">The type of the callback argument.</typeparam>
        /// <param name="callback">The action callback to be converted.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Action<TValue> AsNonRenderingEventHandler<TValue>(Action<TValue> callback)
            => new SyncReceiver<TValue>(callback).Invoke;

        /// <summary>
        /// Converts the provided <see cref="Func{Task}"/> callback into a non-rendering event handler.
        /// </summary>
        /// <param name="callback">The asynchronous callback to be converted.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Func<Task> AsNonRenderingEventHandler(Func<Task> callback)
            => new AsyncReceiver(callback).Invoke;

        /// <summary>
        /// Converts the provided <see cref="Func{TValue, Task}"/> callback into a non-rendering event handler.
        /// </summary>
        /// <typeparam name="TValue">The type of the callback argument.</typeparam>
        /// <param name="callback">The asynchronous callback to be converted.</param>
        /// <returns>A non-rendering event handler.</returns>
        public static Func<TValue, Task> AsNonRenderingEventHandler<TValue>(Func<TValue, Task> callback)
            => new AsyncReceiver<TValue>(callback).Invoke;

        private record SyncReceiver(Action Callback) : ReceiverBase
        {
            public void Invoke() => Callback();
        }

        private record SyncReceiver<T>(Action<T> Callback) : ReceiverBase
        {
            public void Invoke(T arg) => Callback(arg);
        }

        private record AsyncReceiver(Func<Task> Callback) : ReceiverBase
        {
            public Task Invoke() => Callback();
        }

        private record AsyncReceiver<T>(Func<T, Task> Callback) : ReceiverBase
        {
            public Task Invoke(T arg) => Callback(arg);
        }

        private record ReceiverBase : IHandleEvent
        {
            public Task HandleEventAsync(EventCallbackWorkItem item, object? arg)
                => item.InvokeAsync(arg);
        }
    }
}
