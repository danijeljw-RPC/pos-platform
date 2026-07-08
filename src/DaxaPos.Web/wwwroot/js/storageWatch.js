// PLAN-0007 Milestone D: bridges the native browser `storage` event (which fires in other tabs of
// the same origin when localStorage changes, never in the tab that made the change) to a .NET
// callback. One handler per watched key, tracked here so `unsubscribe` can remove the exact listener
// `subscribe` added.
const listeners = new Map();

export function subscribe(key, dotNetRef) {
    const handler = (event) => {
        if (event.key === key) {
            dotNetRef.invokeMethodAsync('OnStorageChangedElsewhere', event.key);
        }
    };

    listeners.set(key, handler);
    window.addEventListener('storage', handler);
}

export function unsubscribe(key) {
    const handler = listeners.get(key);
    if (handler) {
        window.removeEventListener('storage', handler);
        listeners.delete(key);
    }
}
