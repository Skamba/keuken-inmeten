const instances = new WeakMap();
const MIN_SCALE = 1;
const MAX_SCALE = 6;
const ZOOM_FACTOR = 1.2;
const EDGE_PADDING = 40;

function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
}

function getMetrics(stage, canvas) {
    return {
        stageWidth: stage.clientWidth,
        stageHeight: stage.clientHeight,
        contentWidth: canvas.offsetWidth,
        contentHeight: canvas.offsetHeight
    };
}

function clampPosition(state) {
    const { stageWidth, stageHeight, contentWidth, contentHeight } = getMetrics(state.stage, state.canvas);
    const scaledWidth = contentWidth * state.scale;
    const scaledHeight = contentHeight * state.scale;

    if (scaledWidth <= stageWidth) {
        state.x = (stageWidth - scaledWidth) / 2;
    } else {
        state.x = clamp(state.x, stageWidth - scaledWidth - EDGE_PADDING, EDGE_PADDING);
    }

    if (scaledHeight <= stageHeight) {
        state.y = (stageHeight - scaledHeight) / 2;
    } else {
        state.y = clamp(state.y, stageHeight - scaledHeight - EDGE_PADDING, EDGE_PADDING);
    }
}

function applyTransform(state) {
    clampPosition(state);
    state.canvas.style.transform = `translate(${state.x}px, ${state.y}px) scale(${state.scale})`;
}

function centerContent(state) {
    const { stageWidth, stageHeight, contentWidth, contentHeight } = getMetrics(state.stage, state.canvas);
    state.x = (stageWidth - contentWidth * state.scale) / 2;
    state.y = (stageHeight - contentHeight * state.scale) / 2;
    applyTransform(state);
}

function zoomAtPoint(state, clientX, clientY, factor) {
    const rect = state.stage.getBoundingClientRect();
    const pointX = clientX - rect.left;
    const pointY = clientY - rect.top;
    const nextScale = clamp(state.scale * factor, MIN_SCALE, MAX_SCALE);

    if (nextScale === state.scale) {
        return;
    }

    const contentX = (pointX - state.x) / state.scale;
    const contentY = (pointY - state.y) / state.scale;

    state.scale = nextScale;
    state.x = pointX - contentX * state.scale;
    state.y = pointY - contentY * state.scale;
    applyTransform(state);
}

function createInstance(stage, canvas, focusTarget) {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const state = {
        stage,
        canvas,
        focusTarget,
        previousOverflow,
        scale: 1,
        x: 0,
        y: 0,
        drag: null
    };

    function onWheel(event) {
        event.preventDefault();
        const factor = event.deltaY < 0 ? ZOOM_FACTOR : 1 / ZOOM_FACTOR;
        zoomAtPoint(state, event.clientX, event.clientY, factor);
    }

    function onPointerDown(event) {
        state.drag = {
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            originX: state.x,
            originY: state.y
        };

        stage.classList.add("panning");
        stage.setPointerCapture(event.pointerId);
        event.preventDefault();
    }

    function onPointerMove(event) {
        if (!state.drag || state.drag.pointerId !== event.pointerId) {
            return;
        }

        state.x = state.drag.originX + (event.clientX - state.drag.startX);
        state.y = state.drag.originY + (event.clientY - state.drag.startY);
        applyTransform(state);
        event.preventDefault();
    }

    function stopDrag(event) {
        if (!state.drag || state.drag.pointerId !== event.pointerId) {
            return;
        }

        stage.classList.remove("panning");
        stage.releasePointerCapture(event.pointerId);
        state.drag = null;
    }

    function onResize() {
        applyTransform(state);
    }

    centerContent(state);
    focusTarget?.focus();

    stage.addEventListener("wheel", onWheel, { passive: false });
    stage.addEventListener("pointerdown", onPointerDown);
    stage.addEventListener("pointermove", onPointerMove);
    stage.addEventListener("pointerup", stopDrag);
    stage.addEventListener("pointercancel", stopDrag);
    window.addEventListener("resize", onResize);

    return {
        state,
        onWheel,
        onPointerDown,
        onPointerMove,
        stopDrag,
        onResize
    };
}

export function init(stage, canvas, focusTarget) {
    dispose(stage);
    instances.set(stage, createInstance(stage, canvas, focusTarget));
}

export function zoomIn(stage) {
    const instance = instances.get(stage);
    if (!instance) {
        return;
    }

    const rect = instance.state.stage.getBoundingClientRect();
    zoomAtPoint(instance.state, rect.left + rect.width / 2, rect.top + rect.height / 2, ZOOM_FACTOR);
}

export function zoomOut(stage) {
    const instance = instances.get(stage);
    if (!instance) {
        return;
    }

    const rect = instance.state.stage.getBoundingClientRect();
    zoomAtPoint(instance.state, rect.left + rect.width / 2, rect.top + rect.height / 2, 1 / ZOOM_FACTOR);
}

export function reset(stage) {
    const instance = instances.get(stage);
    if (!instance) {
        return;
    }

    instance.state.scale = 1;
    centerContent(instance.state);
}

export function dispose(stage) {
    const instance = instances.get(stage);
    if (!instance) {
        return;
    }

    stage.removeEventListener("wheel", instance.onWheel);
    stage.removeEventListener("pointerdown", instance.onPointerDown);
    stage.removeEventListener("pointermove", instance.onPointerMove);
    stage.removeEventListener("pointerup", instance.stopDrag);
    stage.removeEventListener("pointercancel", instance.stopDrag);
    stage.classList.remove("panning");
    window.removeEventListener("resize", instance.onResize);
    document.body.style.overflow = instance.state.previousOverflow;
    instances.delete(stage);
}
