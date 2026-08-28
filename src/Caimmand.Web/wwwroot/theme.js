// Caimmand theme manager.
// Runs synchronously in <head> before first paint to avoid a flash of the wrong theme.
// Resolution: explicit user choice (localStorage) > prefers-color-scheme.
(function () {
    var KEY = "caimmand-theme";

    function resolve() {
        try {
            var stored = localStorage.getItem(KEY);
            if (stored === "light" || stored === "dark") return stored;
        } catch (e) {
            // localStorage unavailable (e.g. blocked cookies) — fall through to system preference.
        }
        return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function apply(theme) {
        document.documentElement.dataset.bsTheme = theme;
        document.documentElement.style.colorScheme = theme;
    }

    apply(resolve());

    window.caimmandTheme = {
        toggle: function () {
            var current = document.documentElement.dataset.bsTheme === "dark" ? "dark" : "light";
            var next = current === "dark" ? "light" : "dark";
            try {
                localStorage.setItem(KEY, next);
            } catch (e) {
                // ignore storage errors; theme still applies for this session
            }
            apply(next);
        },
        current: function () {
            return document.documentElement.dataset.bsTheme === "dark" ? "dark" : "light";
        }
    };

    // Follow OS-level theme changes only while the user hasn't picked explicitly.
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function (e) {
        try {
            if (localStorage.getItem(KEY) === null) {
                apply(e.matches ? "dark" : "light");
            }
        } catch (err) {
            // ignore storage errors
        }
    });
})();
