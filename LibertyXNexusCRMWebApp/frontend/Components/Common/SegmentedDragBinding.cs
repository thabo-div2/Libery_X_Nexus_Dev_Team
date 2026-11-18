using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace frontend.Components.Common;

/// <summary>
/// Makes a sliding segmented control (a <c>.seg-thumb</c> inside a container) draggable.
/// The page owns one instance, calls <see cref="BindAsync"/> after each render and disposes it with the page.
/// wwwroot/js/segmented-drag.js moves the thumb while dragging, then reports the chosen option index back here.
/// </summary>
public sealed class SegmentedDragBinding : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Func<int, Task> _onPick;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SegmentedDragBinding>? _self;
    private string? _boundId;

    public SegmentedDragBinding(IJSRuntime js, Func<int, Task> onPick)
    {
        _js = js;
        _onPick = onPick;
    }

    /// <summary>Attaches drag handling to the container. Safe to call every render: it only acts when the element is new.</summary>
    public async Task BindAsync(ElementReference container)
    {
        if (container.Context is null || container.Id is null || container.Id == _boundId) return;
        _boundId = container.Id;

        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/segmented-drag.js");
            _self ??= DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("init", container, _self, nameof(Pick));
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    [JSInvokable]
    public Task Pick(int index) => _onPick(index);

    public async ValueTask DisposeAsync()
    {
        _self?.Dispose();
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
        }
    }
}
