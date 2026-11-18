// Lets the sliding thumb of a segmented control (.pipeline-filters) be dragged as well as clicked.
// The thumb follows the pointer, then snaps to the nearest option and tells Blazor which one was chosen.
export function init(container, dotnet, method = 'SetInstitutionByIndex') {
    if (!container || container.__segDrag) return;
    container.__segDrag = true;

    const thumb = container.querySelector('.seg-thumb');
    if (!thumb) return;

    let state = null;
    let suppressClick = false;

    const metrics = () => {
        const css = getComputedStyle(container);
        return {
            n: parseInt(css.getPropertyValue('--n'), 10) || 1,
            i: parseInt(css.getPropertyValue('--i'), 10) || 0,
            w: thumb.getBoundingClientRect().width
        };
    };

    const markTarget = (index) => {
        container.querySelectorAll('.filter-pill').forEach((pill, k) => pill.classList.toggle('drag-target', k === index));
    };

    container.addEventListener('pointerdown', (e) => {
        if (e.pointerType === 'mouse' && e.button !== 0) return;
        const m = metrics();
        if (m.n < 2) return;
        state = { id: e.pointerId, x0: e.clientX, drag: false, pos: m.i * m.w, ...m };
    });

    container.addEventListener('pointermove', (e) => {
        if (!state || e.pointerId !== state.id) return;
        const dx = e.clientX - state.x0;
        if (!state.drag) {
            if (Math.abs(dx) < 5) return;
            state.drag = true;
            try { container.setPointerCapture(state.id); } catch { }
            container.classList.add('is-dragging');
        }
        const max = (state.n - 1) * state.w;
        state.pos = Math.min(max, Math.max(0, state.i * state.w + dx));
        thumb.style.transform = 'translateX(' + state.pos + 'px)';
        markTarget(Math.round(state.pos / state.w));
    });

    const finish = (e) => {
        if (!state || e.pointerId !== state.id) return;
        const s = state;
        state = null;
        if (!s.drag) return;

        suppressClick = true;
        setTimeout(() => { suppressClick = false; }, 60);
        try { container.releasePointerCapture(s.id); } catch { }
        container.classList.remove('is-dragging');
        markTarget(-1);

        const index = Math.min(s.n - 1, Math.max(0, Math.round(s.pos / s.w)));
        thumb.style.transform = 'translateX(' + index * s.w + 'px)';

        const release = (tries) => {
            if (metrics().i === index || tries > 40) thumb.style.transform = '';
            else requestAnimationFrame(() => release(tries + 1));
        };

        if (index === s.i) {
            thumb.style.transform = '';
        } else {
            dotnet.invokeMethodAsync(method, index).then(() => release(0), () => { thumb.style.transform = ''; });
        }
    };

    container.addEventListener('pointerup', finish);
    container.addEventListener('pointercancel', finish);

    // A drag ends with a click on whatever is under the pointer; swallow it so it doesn't re-pick an option.
    container.addEventListener('click', (e) => {
        if (suppressClick) { e.stopPropagation(); e.preventDefault(); }
    }, true);
}
