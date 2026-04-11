const DRAG_THRESHOLD = 5;
const instances = new WeakMap();

function createInstance(svgEl, dotNetRef) {
    let dragState = null;

    function getSvgPoint(e) {
        const pt = svgEl.createSVGPoint();
        pt.x = e.clientX;
        pt.y = e.clientY;
        return pt.matrixTransform(svgEl.getScreenCTM().inverse());
    }

    function findGroup(el, className) {
        while (el && el !== svgEl) {
            if (el.classList && el.classList.contains(className)) return el;
            el = el.parentElement;
        }
        return null;
    }

    function parseRect(group) {
        return {
            x: parseFloat(group.dataset.x),
            y: parseFloat(group.dataset.y),
            width: parseFloat(group.dataset.width),
            height: parseFloat(group.dataset.height)
        };
    }

    function findKastInTree(el) {
        let current = el;
        while (current && current !== svgEl) {
            if (current.classList && current.classList.contains('paneel-kast-selecteerbaar')) {
                return current;
            }

            current = current.parentNode;
        }

        return null;
    }

    function pointIsInsideRect(clientX, clientY, rect) {
        return clientX >= rect.left
            && clientX <= rect.right
            && clientY >= rect.top
            && clientY <= rect.bottom;
    }

    function findKastAtPoint(clientX, clientY, ignoredGroup) {
        const elements = document.elementsFromPoint(clientX, clientY);
        for (const element of elements) {
            if (ignoredGroup && (element === ignoredGroup || ignoredGroup.contains(element))) {
                continue;
            }

            const kast = findKastInTree(element);
            if (kast?.dataset.kastId) {
                return kast;
            }
        }

        const kasten = svgEl.querySelectorAll('.paneel-kast-selecteerbaar');
        for (const kast of kasten) {
            const body = kast.querySelector('rect');
            if (!body) {
                continue;
            }

            const rect = body.getBoundingClientRect();
            if (pointIsInsideRect(clientX, clientY, rect)) {
                return kast;
            }
        }

        return null;
    }

    function setAttr(el, name, value) {
        if (el) el.setAttribute(name, `${value}`);
    }

    function updatePreview(group, rect) {
        group.dataset.x = `${rect.x}`;
        group.dataset.y = `${rect.y}`;
        group.dataset.width = `${rect.width}`;
        group.dataset.height = `${rect.height}`;

        const body = group.querySelector('.paneel-draft-body');
        const header = group.querySelector('.paneel-draft-header');
        const subtitle = group.querySelector('.paneel-draft-subtitle');
        const hinge = group.querySelector('.paneel-draft-hinge');
        const title = group.querySelector('.paneel-draft-title');

        const labelHeight = Math.min(18, rect.height * 0.24);
        setAttr(body, 'x', rect.x);
        setAttr(body, 'y', rect.y);
        setAttr(body, 'width', rect.width);
        setAttr(body, 'height', rect.height);

        setAttr(header, 'x', rect.x);
        setAttr(header, 'y', rect.y);
        setAttr(header, 'width', rect.width);
        setAttr(header, 'height', labelHeight);

        setAttr(title, 'x', rect.x + rect.width / 2);
        setAttr(title, 'y', rect.y + Math.max(11, labelHeight * 0.72));

        setAttr(subtitle, 'x', rect.x + rect.width / 2);
        setAttr(subtitle, 'y', Math.min(rect.y + rect.height - 10, rect.y + labelHeight + 16));

        if (hinge) {
            const isLeft = Number(hinge.getAttribute('x1')) < rect.x + rect.width / 2;
            const hingeX = isLeft ? rect.x + 7 : rect.x + rect.width - 7;
            setAttr(hinge, 'x1', hingeX);
            setAttr(hinge, 'x2', hingeX);
            setAttr(hinge, 'y1', rect.y + labelHeight + 5);
            setAttr(hinge, 'y2', rect.y + rect.height - 5);
        }

        const handles = {
            nw: [rect.x, rect.y],
            ne: [rect.x + rect.width, rect.y],
            se: [rect.x + rect.width, rect.y + rect.height],
            sw: [rect.x, rect.y + rect.height]
        };

        group.querySelectorAll('.paneel-draft-handle').forEach(handle => {
            const key = handle.dataset.handle;
            const [x, y] = handles[key];
            setAttr(handle, 'cx', x);
            setAttr(handle, 'cy', y);
        });
    }

    function onPointerDown(e) {
        const handle = e.target.closest?.('.paneel-draft-handle');
        const paneel = handle ? findGroup(handle, 'paneel-draft') : findGroup(e.target, 'paneel-draft');
        if (paneel) {
            const rect = parseRect(paneel);
            const pt = getSvgPoint(e);
            dragState = {
                type: 'paneel',
                mode: handle ? handle.dataset.handle : 'move',
                group: paneel,
                rect,
                startPoint: pt,
                moved: false
            };
            svgEl.setPointerCapture(e.pointerId);
            e.preventDefault();
            e.stopPropagation();
            return;
        }

        const kast = findGroup(e.target, 'paneel-kast-selecteerbaar');
        if (kast?.dataset.kastId) {
            dragState = {
                type: 'kast-click',
                kastId: kast.dataset.kastId,
                startClientX: e.clientX,
                startClientY: e.clientY
            };
        }
    }

    function onPointerMove(e) {
        if (!dragState || dragState.type !== 'paneel') return;

        const pt = getSvgPoint(e);
        const dx = pt.x - dragState.startPoint.x;
        const dy = pt.y - dragState.startPoint.y;
        if (!dragState.moved && Math.hypot(dx, dy) < DRAG_THRESHOLD) return;

        dragState.moved = true;
        const start = dragState.rect;
        const next = { ...start };

        switch (dragState.mode) {
            case 'move':
                next.x = start.x + dx;
                next.y = start.y + dy;
                break;
            case 'nw':
                next.x = start.x + dx;
                next.y = start.y + dy;
                next.width = start.width - dx;
                next.height = start.height - dy;
                break;
            case 'ne':
                next.y = start.y + dy;
                next.width = start.width + dx;
                next.height = start.height - dy;
                break;
            case 'se':
                next.width = start.width + dx;
                next.height = start.height + dy;
                break;
            case 'sw':
                next.x = start.x + dx;
                next.width = start.width - dx;
                next.height = start.height + dy;
                break;
        }

        next.width = Math.max(next.width, 6);
        next.height = Math.max(next.height, 6);
        dragState.preview = next;
        updatePreview(dragState.group, next);
        e.preventDefault();
    }

    function onPointerUp(e) {
        if (!dragState) return;

        if (dragState.type === 'paneel') {
            svgEl.releasePointerCapture(e.pointerId);
            if (dragState.moved) {
                const rect = dragState.preview ?? dragState.rect;
                dotNetRef.invokeMethodAsync('OnConceptPaneelUpdate', dragState.mode, rect.x, rect.y, rect.width, rect.height);
            }
            else if (dragState.mode === 'move') {
                const kast = findKastAtPoint(e.clientX, e.clientY, dragState.group);
                if (kast?.dataset.kastId) {
                    dotNetRef.invokeMethodAsync('OnKastKlik', kast.dataset.kastId);
                }
            }
            dragState = null;
            e.preventDefault();
            e.stopPropagation();
            return;
        }

        if (dragState.type === 'kast-click') {
            const moved = Math.hypot(e.clientX - dragState.startClientX, e.clientY - dragState.startClientY) >= DRAG_THRESHOLD;
            if (!moved) {
                dotNetRef.invokeMethodAsync('OnKastKlik', dragState.kastId);
            }
        }

        dragState = null;
    }

    function onClick(e) {
        if (findGroup(e.target, 'paneel-draft')) {
            e.preventDefault();
            e.stopPropagation();
        }
    }

    return { onPointerDown, onPointerMove, onPointerUp, onClick };
}

export function init(svgEl, dotNetRef) {
    const handlers = createInstance(svgEl, dotNetRef);
    instances.set(svgEl, handlers);
    svgEl.addEventListener('pointerdown', handlers.onPointerDown);
    svgEl.addEventListener('pointermove', handlers.onPointerMove);
    svgEl.addEventListener('pointerup', handlers.onPointerUp);
    svgEl.addEventListener('click', handlers.onClick);
}

export function dispose(svgEl) {
    const handlers = instances.get(svgEl);
    if (!handlers) return;
    svgEl.removeEventListener('pointerdown', handlers.onPointerDown);
    svgEl.removeEventListener('pointermove', handlers.onPointerMove);
    svgEl.removeEventListener('pointerup', handlers.onPointerUp);
    svgEl.removeEventListener('click', handlers.onClick);
    instances.delete(svgEl);
}
