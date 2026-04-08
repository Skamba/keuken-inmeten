function currentTheme() {
    return document.documentElement.getAttribute("data-bs-theme") || "light";
}

export function getTheme() {
    return currentTheme();
}

export function toggleTheme() {
    const next = currentTheme() === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-bs-theme", next);
    localStorage.setItem("keuken-theme", next);
    return next;
}
