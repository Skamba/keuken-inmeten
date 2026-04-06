const DRAG_THRESHOLD = 5; // px — less than this is treated as a click

// Per-instance state stored per SVG element to support multiple instances on one page.
const instances = new WeakMap();

function createInstance(svgEl, dotNetRef, leesAlleen = false) {
    let dragging = null;
    let lastUpTime = 0;
    let lastUpX = 0;
    let lastUpY = 0;
    let lastUpKastId = null;
    let lastUpPlankTime = 0;
    let lastUpPlankX = 0;
    let lastUpPlankY = 0;
    let lastUpPlankId = null;

    function getSvgPoint(e) {
        const pt = svgEl.createSVGPoint();
        pt.x = e.clientX;
        pt.y = e.clientY;
        return pt.matrixTransform(svgEl.getScreenCTM().inverse());
    }

    function findPlankGroup(el) {
        while (el && el !== svgEl) {
            if (el.classList && el.classList.contains('wand-plank-sleepbaar')) return el;
            el = el.parentElement;
        }
        return null;
    }

    function findKastGroup(el) {
        while (el && el !== svgEl) {
            if (el.classList && el.classList.contains('wand-kast-sleepbaar')) return el;
            el = el.parentElement;
        }
        return null;
    }

    function onPointerDown(e) {
        if (leesAlleen) return;
        const plankG = findPlankGroup(e.target);
        if (plankG) {
            const kastId = plankG.dataset.kastId;
            const plankId = plankG.dataset.plankId;
            if (!kastId || !plankId) return;

            const pt = getSvgPoint(e);
            const rect = plankG.querySelector('rect');
            const ry = parseFloat(rect.getAttribute('y'));
            const rh = parseFloat(rect.getAttribute('height'));
            const centerY = ry + rh / 2;

            dragging = {
                type: 'plank', kastId, plankId, g: plankG,
                offsetY: pt.y - centerY, startCenterY: centerY,
                startClientX: e.clientX, startClientY: e.clientY, moved: false
            };
            plankG.style.cursor = 'grabbing';
            svgEl.setPointerCapture(e.pointerId);
            e.preventDefault();
            return;
        }

        const kastG = findKastGroup(e.target);
        if (!kastG) return;
        const kastId = kastG.dataset.kastId;
        if (!kastId) return;

        const pt = getSvgPoint(e);
        const rect = kastG.querySelector('rect');
        const rx = parseFloat(rect.getAttribute('x'));
        const ry = parseFloat(rect.getAttribute('y'));

        dragging = {
            type: 'kast', kastId, g: kastG,
            offsetX: pt.x - rx, offsetY: pt.y - ry,
            startX: rx, startY: ry,
            startClientX: e.clientX, startClientY: e.clientY, moved: false
        };
        kastG.style.cursor = 'grabbing';
        svgEl.setPointerCapture(e.pointerId);
        e.preventDefault();
    }

    function onPointerMove(e) {
        if (!dragging) return;
        const dx = e.clientX - dragging.startClientX;
        const dy = e.clientY - dragging.startClientY;
        if (!dragging.moved && Math.hypot(dx, dy) < DRAG_THRESHOLD) return;
        dragging.moved = true;

        const pt = getSvgPoint(e);
        if (dragging.type === 'plank') {
            const centerY = pt.y - dragging.offsetY;
            dragging.g.setAttribute('transform', `translate(0, ${centerY - dragging.startCenterY})`);

            // Live label update
            const kastG = dragging.g.closest('.wand-kast-sleepbaar');
            if (kastG) {
                const botY  = parseFloat(kastG.dataset.botY);
                const wdPx  = parseFloat(kastG.dataset.wdPx);
                const schaal = parseFloat(kastG.dataset.schaal);
                if (schaal > 0) {
                    const mm = Math.max(0, (botY - wdPx - centerY) / schaal);
                    const lbl = dragging.g.querySelector('.plank-label');
                    if (lbl) lbl.textContent = `${Math.round(mm)} mm`;
                }
            }
        } else {
            const nx = pt.x - dragging.offsetX;
            const ny = pt.y - dragging.offsetY;
            dragging.g.setAttribute('transform', `translate(${nx - dragging.startX}, ${ny - dragging.startY})`);
        }
    }

    function onPointerUp(e) {
        if (!dragging) return;
        const { type, g, moved } = dragging;
        g.style.cursor = type === 'plank' ? 'ns-resize' : 'grab';
        g.removeAttribute('transform');
        svgEl.releasePointerCapture(e.pointerId);

        const pt = getSvgPoint(e);

        if (type === 'plank') {
            if (moved) {
                dotNetRef.invokeMethodAsync('OnPlankDrop', dragging.kastId, dragging.plankId, pt.y - dragging.offsetY);
                lastUpPlankTime = 0;
            } else {
                const now = Date.now();
                const isDoubleTap = (now - lastUpPlankTime < 400) &&
                                    (lastUpPlankId === dragging.plankId) &&
                                    (Math.hypot(e.clientX - lastUpPlankX, e.clientY - lastUpPlankY) < 20);
                if (isDoubleTap) {
                    dotNetRef.invokeMethodAsync('OnPlankVerwijderen', dragging.kastId, dragging.plankId);
                    lastUpPlankTime = 0;
                    lastUpPlankId = null;
                } else {
                    dotNetRef.invokeMethodAsync('OnPlankKlik', dragging.kastId, dragging.plankId);
                    lastUpPlankTime = now;
                    lastUpPlankX = e.clientX;
                    lastUpPlankY = e.clientY;
                    lastUpPlankId = dragging.plankId;
                }
            }
            lastUpTime = 0;
        } else {
            if (moved) {
                dotNetRef.invokeMethodAsync('OnDrop', dragging.kastId, pt.x - dragging.offsetX, pt.y - dragging.offsetY);
                lastUpTime = 0;
            } else {
                const now = Date.now();
                const isDoubleTap = (now - lastUpTime < 400) &&
                                    (lastUpKastId === dragging.kastId) &&
                                    (Math.hypot(e.clientX - lastUpX, e.clientY - lastUpY) < 20);
                if (isDoubleTap) {
                    dotNetRef.invokeMethodAsync('OnPlankToevoegen', dragging.kastId, pt.y);
                    lastUpTime = 0;
                    lastUpKastId = null;
                } else {
                    dotNetRef.invokeMethodAsync('OnKastKlik', dragging.kastId);
                    lastUpTime = now;
                    lastUpX = e.clientX;
                    lastUpY = e.clientY;
                    lastUpKastId = dragging.kastId;
                }
            }
        }
        dragging = null;
    }

    function onClick(e) {
        if (!leesAlleen) return;
        const kastG = findKastGroup(e.target);
        if (kastG?.dataset.kastId) {
            dotNetRef.invokeMethodAsync('OnKastKlik', kastG.dataset.kastId);
        }
    }

    function onKeyDown(e) {
        if (leesAlleen) return;
        if (e.ctrlKey && (e.key === 'c' || e.key === 'C')) {
            if (lastUpKastId) { dotNetRef.invokeMethodAsync('OnKopierenToets', lastUpKastId); e.preventDefault(); }
            return;
        }
        if (e.ctrlKey && (e.key === 'v' || e.key === 'V')) {
            dotNetRef.invokeMethodAsync('OnPlakkenToets');
            e.preventDefault();
            return;
        }
        if (!['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Delete'].includes(e.key)) return;
        e.preventDefault();
        dotNetRef.invokeMethodAsync('OnToets', e.key, e.shiftKey ? 10 : 1);
    }

    return { onPointerDown, onPointerMove, onPointerUp, onKeyDown, onClick };
}

export function init(svgEl, dotNetRef, leesAlleen = false) {
    const handlers = createInstance(svgEl, dotNetRef, leesAlleen);
    instances.set(svgEl, handlers);
    svgEl.addEventListener('pointerdown', handlers.onPointerDown);
    svgEl.addEventListener('pointermove', handlers.onPointerMove);
    svgEl.addEventListener('pointerup', handlers.onPointerUp);
    svgEl.addEventListener('click', handlers.onClick);
    svgEl.addEventListener('keydown', handlers.onKeyDown);
}

export function dispose(svgEl) {
    const handlers = instances.get(svgEl);
    if (!handlers) return;
    svgEl.removeEventListener('pointerdown', handlers.onPointerDown);
    svgEl.removeEventListener('pointermove', handlers.onPointerMove);
    svgEl.removeEventListener('pointerup', handlers.onPointerUp);
    svgEl.removeEventListener('click', handlers.onClick);
    svgEl.removeEventListener('keydown', handlers.onKeyDown);
    instances.delete(svgEl);
}
