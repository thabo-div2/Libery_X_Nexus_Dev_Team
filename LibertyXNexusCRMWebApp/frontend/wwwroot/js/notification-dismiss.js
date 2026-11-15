const listeners = new WeakMap();
export function bind(root, receiver) {
    unbind(root);
    const abort = new AbortController();
    document.addEventListener('pointerdown', event => {
        if (!root.isConnected || event.composedPath().includes(root)) return;
        if (root.querySelector('.notification-button')?.getAttribute('aria-expanded') !== 'true') return;
        receiver.invokeMethodAsync('DismissOutsideAsync').catch(() => {});
    }, { capture: true, signal: abort.signal });
    listeners.set(root, abort);
}
export function unbind(root) {
    listeners.get(root)?.abort();
    listeners.delete(root);
}
