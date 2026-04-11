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

    function parsePlankSnaps(kastG) {
        const raw = kastG?.dataset?.plankSnaps;
        if (!raw) return [];

        return raw
            .split('|')
            .map(item => {
                const [heightStr, holeIndexStr] = item.split(':');
                const height = parseFloat(heightStr);
                const holeIndex = parseInt(holeIndexStr, 10);
                return Number.isFinite(height) && Number.isFinite(holeIndex)
                    ? { height, holeIndex }
                    : null;
            })
            .filter(Boolean);
    }

    function formatMm(value) {
        const rounded = Math.round(value * 10) / 10;
        return Number.isInteger(rounded) ? `${rounded}` : rounded.toFixed(1);
    }

    function formatPlankLabel(height, holeIndex) {
        return holeIndex ? `${formatMm(height)} mm | gat ${holeIndex}` : `${formatMm(height)} mm`;
    }

    function setSvgNumberAttribute(el, name, value) {
        if (!el || !Number.isFinite(value)) return;
        const normalized = Math.round(value * 1000) / 1000;
        el.setAttribute(name, `${normalized}`);
    }

    function settlePlankGeometry(dragState, centerY, labelText) {
        if (!dragState?.g) return;

        const offsetY = centerY - dragState.startCenterY;
        const rect = dragState.g.querySelector('rect');
        if (rect) {
            setSvgNumberAttribute(rect, 'y', dragState.startRectY + offsetY);
        }

        const lbl = dragState.g.querySelector('.plank-label');
        if (lbl) {
            if (Number.isFinite(dragState.startLabelY)) {
                setSvgNumberAttribute(lbl, 'y', dragState.startLabelY + offsetY);
            }

            if (typeof labelText === 'string') {
                lbl.textContent = labelText;
            }
        }

        dragState.g.removeAttribute('transform');
    }

    function getPlankHeightRange(dragState) {
        if (!dragState || !(dragState.schaal > 0)) {
            return { minHeight: 0, maxHeight: 0 };
        }

        if (dragState.snapTargets.length) {
            const heights = dragState.snapTargets.map(candidate => candidate.height);
            return {
                minHeight: Math.min(...heights),
                maxHeight: Math.max(...heights)
            };
        }

        const maxHeight = Math.max(0, (dragState.botY - dragState.wdPx) / dragState.schaal);
        return { minHeight: 0, maxHeight };
    }

    function clampPlankCenterY(dragState, rawCenterY) {
        if (!dragState || !(dragState.schaal > 0)) {
            return rawCenterY;
        }

        const { minHeight, maxHeight } = getPlankHeightRange(dragState);
        const rawHeight = (dragState.botY - dragState.wdPx - rawCenterY) / dragState.schaal;
        const clampedHeight = Math.min(maxHeight, Math.max(minHeight, rawHeight));
        return dragState.botY - dragState.wdPx - clampedHeight * dragState.schaal;
    }

    function getPlankSnapPreview(dragState, rawCenterY) {
        if (!dragState || !(dragState.schaal > 0)) {
            return { visualCenterY: rawCenterY, centerY: rawCenterY, height: 0, holeIndex: null };
        }

        const visualCenterY = clampPlankCenterY(dragState, rawCenterY);
        const rawHeight = Math.max(0, (dragState.botY - dragState.wdPx - visualCenterY) / dragState.schaal);
        if (!dragState.snapTargets.length) {
            return { visualCenterY, centerY: visualCenterY, height: rawHeight, holeIndex: null };
        }

        const best = dragState.snapTargets.reduce((closest, candidate) =>
            Math.abs(candidate.height - rawHeight) < Math.abs(closest.height - rawHeight) ? candidate : closest);
        const snappedCenterY = dragState.botY - dragState.wdPx - best.height * dragState.schaal;

        return { visualCenterY, centerY: snappedCenterY, height: best.height, holeIndex: best.holeIndex };
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
            const lbl = plankG.querySelector('.plank-label');
            const kastG = plankG.closest('.wand-kast-sleepbaar');
            const ry = parseFloat(rect.getAttribute('y'));
            const rh = parseFloat(rect.getAttribute('height'));
            const labelY = parseFloat(lbl?.getAttribute('y'));
            const centerY = ry + rh / 2;
            const botY = parseFloat(kastG?.dataset?.botY);
            const wdPx = parseFloat(kastG?.dataset?.wdPx);
            const schaal = parseFloat(kastG?.dataset?.schaal);
            const initialSnap = getPlankSnapPreview({ botY, wdPx, schaal, snapTargets: parsePlankSnaps(kastG) }, centerY);

            dragging = {
                type: 'plank', kastId, plankId, g: plankG,
                offsetY: pt.y - centerY, startCenterY: centerY,
                startClientX: e.clientX, startClientY: e.clientY, moved: false,
                startRectY: ry,
                startLabelY: labelY,
                startLabelText: lbl?.textContent ?? '',
                kastG, botY, wdPx, schaal,
                snapTargets: parsePlankSnaps(kastG),
                visualCenterY: initialSnap.visualCenterY,
                snappedCenterY: initialSnap.centerY,
                snappedHeight: initialSnap.height,
                snappedHoleIndex: initialSnap.holeIndex
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
            const rawCenterY = pt.y - dragging.offsetY;
            const snap = getPlankSnapPreview(dragging, rawCenterY);
            dragging.visualCenterY = snap.visualCenterY;
            dragging.snappedCenterY = snap.centerY;
            dragging.snappedHeight = snap.height;
            dragging.snappedHoleIndex = snap.holeIndex;
            dragging.g.setAttribute('transform', `translate(0, ${snap.visualCenterY - dragging.startCenterY})`);

            // Live label update
            const lbl = dragging.g.querySelector('.plank-label');
            if (lbl) lbl.textContent = formatPlankLabel(snap.height, snap.holeIndex);
        } else {
            const nx = pt.x - dragging.offsetX;
            const ny = pt.y - dragging.offsetY;
            dragging.g.setAttribute('transform', `translate(${nx - dragging.startX}, ${ny - dragging.startY})`);
        }
    }

    function onPointerUp(e) {
        if (!dragging) return;
        const completedDrag = dragging;
        const { type, g, moved } = completedDrag;
        g.style.cursor = type === 'plank' ? 'ns-resize' : 'grab';
        svgEl.releasePointerCapture(e.pointerId);

        const pt = getSvgPoint(e);

        if (type === 'plank') {
            if (moved) {
                const finalCenterY = completedDrag.snappedCenterY ?? completedDrag.startCenterY;
                const finalHeight = completedDrag.snappedHeight ?? 0;
                const finalHoleIndex = completedDrag.snappedHoleIndex ?? null;
                const finalLabelText = formatPlankLabel(finalHeight, finalHoleIndex);
                g.setAttribute('transform', `translate(0, ${finalCenterY - completedDrag.startCenterY})`);
                const lbl = g.querySelector('.plank-label');
                if (lbl) lbl.textContent = finalLabelText;
                const dropPromise = dotNetRef.invokeMethodAsync('OnPlankDrop', completedDrag.kastId, completedDrag.plankId, finalHeight);
                dropPromise
                    .then(changed => {
                        if (!changed) {
                            settlePlankGeometry(completedDrag, completedDrag.startCenterY, completedDrag.startLabelText);
                            return;
                        }

                        settlePlankGeometry(completedDrag, finalCenterY, finalLabelText);
                    })
                    .catch(() => {
                        settlePlankGeometry(completedDrag, completedDrag.startCenterY, completedDrag.startLabelText);
                    });
                lastUpPlankTime = 0;
            } else {
                g.removeAttribute('transform');
                const now = Date.now();
                const isDoubleTap = (now - lastUpPlankTime < 400) &&
                                    (lastUpPlankId === completedDrag.plankId) &&
                                    (Math.hypot(e.clientX - lastUpPlankX, e.clientY - lastUpPlankY) < 20);
                if (isDoubleTap) {
                    dotNetRef.invokeMethodAsync('OnPlankVerwijderen', completedDrag.kastId, completedDrag.plankId);
                    lastUpPlankTime = 0;
                    lastUpPlankId = null;
                } else {
                    dotNetRef.invokeMethodAsync('OnPlankKlik', completedDrag.kastId, completedDrag.plankId);
                    lastUpPlankTime = now;
                    lastUpPlankX = e.clientX;
                    lastUpPlankY = e.clientY;
                    lastUpPlankId = completedDrag.plankId;
                }
            }
            lastUpTime = 0;
        } else {
            g.removeAttribute('transform');
            if (moved) {
                dotNetRef.invokeMethodAsync('OnDrop', completedDrag.kastId, pt.x - completedDrag.offsetX, pt.y - completedDrag.offsetY);
                lastUpTime = 0;
            } else {
                const now = Date.now();
                const isDoubleTap = (now - lastUpTime < 400) &&
                                    (lastUpKastId === completedDrag.kastId) &&
                                    (Math.hypot(e.clientX - lastUpX, e.clientY - lastUpY) < 20);
                if (isDoubleTap) {
                    dotNetRef.invokeMethodAsync('OnPlankToevoegen', completedDrag.kastId, pt.y);
                    lastUpTime = 0;
                    lastUpKastId = null;
                } else {
                    dotNetRef.invokeMethodAsync('OnKastKlik', completedDrag.kastId);
                    lastUpTime = now;
                    lastUpX = e.clientX;
                    lastUpY = e.clientY;
                    lastUpKastId = completedDrag.kastId;
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
