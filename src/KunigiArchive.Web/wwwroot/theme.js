// Light/dark theme. Loaded in <head> without defer, so the theme is set before the first paint.
// The choice is kept per browser in localStorage; without one, the system setting decides.
(() => {
    const key = "kunigi-theme";
    const root = document.documentElement;
    const media = matchMedia("(prefers-color-scheme: dark)");

    const stored = () => {
        try {
            const value = localStorage.getItem(key);
            return value === "light" || value === "dark" ? value : null;
        } catch {
            return null;
        }
    };

    const current = () => stored() ?? (media.matches ? "dark" : "light");

    // The button is a "dark theme" toggle: pressed while the dark theme is on.
    const syncButtons = () => {
        const dark = String(current() === "dark");
        document.querySelectorAll("[data-theme-toggle]").forEach(button => button.setAttribute("aria-pressed", dark));
    };

    const apply = () => {
        root.setAttribute("data-bs-theme", current());
        syncButtons();
    };

    apply();

    // Delegated, so it keeps working after Blazor's enhanced navigation swaps the page content.
    document.addEventListener("click", event => {
        if (!event.target.closest("[data-theme-toggle]")) {
            return;
        }
        const next = current() === "dark" ? "light" : "dark";
        try {
            localStorage.setItem(key, next);
        } catch {
            // Storage blocked: the switch still works for this page.
            root.setAttribute("data-bs-theme", next);
            document.querySelectorAll("[data-theme-toggle]").forEach(button => button.setAttribute("aria-pressed", String(next === "dark")));
            return;
        }
        apply();
    });

    media.addEventListener("change", apply);

    document.addEventListener("DOMContentLoaded", () => {
        syncButtons();
        // Enhanced navigation syncs the DOM with the server's HTML, which has no data-bs-theme.
        window.Blazor?.addEventListener("enhancedload", apply);
    });
})();
