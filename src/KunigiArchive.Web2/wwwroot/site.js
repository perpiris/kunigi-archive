// Page behavior that needs JavaScript: the rich-text editor and the photo gallery.
// Both libraries load on first use, so pages without an editor or photos don't download them.
// Blazor's enhanced navigation swaps page content without a reload, so everything also runs on "enhancedload".
(() => {
    const scripts = new Map();

    const loadScript = src => {
        if (!scripts.has(src)) {
            scripts.set(src, new Promise((resolve, reject) => {
                const script = document.createElement("script");
                script.src = src;
                script.onload = resolve;
                script.onerror = () => {
                    scripts.delete(src);
                    reject(new Error(`Failed to load ${src}`));
                };
                document.head.append(script);
            }));
        }
        return scripts.get(src);
    };

    // ---------- Rich text ----------
    // <k-rich-text><textarea name="..."></textarea></k-rich-text>
    // The textarea stays the form field: Quill edits a copy and writes the HTML back on every change.
    // Without JavaScript the plain textarea still works, and the server sanitizes whatever arrives.

    const toolbar = [
        [{ header: 2 }, { header: 3 }],
        ["bold", "italic", "underline", "strike"],
        [{ list: "ordered" }, { list: "bullet" }],
        ["blockquote", "link"],
        ["clean"]
    ];

    const titles = {
        "ql-header[value='2']": "Επικεφαλίδα",
        "ql-header[value='3']": "Υποεπικεφαλίδα",
        "ql-bold": "Έντονα",
        "ql-italic": "Πλάγια",
        "ql-underline": "Υπογράμμιση",
        "ql-strike": "Διακριτή διαγραφή",
        "ql-list[value='ordered']": "Αριθμημένη λίστα",
        "ql-list[value='bullet']": "Λίστα με κουκκίδες",
        "ql-blockquote": "Παράθεση",
        "ql-link": "Σύνδεσμος",
        "ql-clean": "Καθαρισμός μορφοποίησης"
    };

    // Descriptions saved before the editor existed are plain text: keep their paragraphs and line breaks.
    const toHtml = value => {
        if (value.includes("<")) {
            return value;
        }
        const escape = text => text.replace(/&/g, "&amp;").replace(/>/g, "&gt;");
        return value.replace(/\r\n/g, "\n").trim().split(/\n\s*\n/)
            .map(paragraph => `<p>${escape(paragraph.trim()).replace(/\n/g, "<br>")}</p>`)
            .join("");
    };

    class RichTextEditor extends HTMLElement {
        connectedCallback() {
            this.mount();
        }

        async mount() {
            const field = this.querySelector("textarea");
            if (!field || this.querySelector(".ql-container")) {
                return;
            }

            try {
                await loadScript("/lib/quill/quill.js");
            } catch {
                return; // The textarea stays usable.
            }
            if (this.querySelector(".ql-container")) {
                return;
            }

            const host = document.createElement("div");
            field.after(host);
            field.hidden = true;

            const quill = new Quill(host, {
                theme: "snow",
                placeholder: field.placeholder,
                modules: { toolbar }
            });

            quill.setContents(quill.clipboard.convert({ html: toHtml(field.value) }), "silent");

            const label = field.id ? document.querySelector(`label[for="${field.id}"]`) : null;
            quill.root.setAttribute("role", "textbox");
            quill.root.setAttribute("aria-multiline", "true");
            if (label) {
                quill.root.setAttribute("aria-label", label.textContent.trim());
                label.addEventListener("click", () => quill.focus());
            }

            for (const [selector, title] of Object.entries(titles)) {
                this.querySelectorAll(`.ql-toolbar button.${selector}`).forEach(button => {
                    button.title = title;
                    button.setAttribute("aria-label", title);
                });
            }

            // root.innerHTML rather than getSemanticHTML(): Quill 2.0.3's semantic HTML turns every space into &nbsp;.
            quill.on("text-change", () => {
                field.value = quill.getText().trim().length === 0 ? "" : quill.root.innerHTML;
            });
        }
    }

    customElements.define("k-rich-text", RichTextEditor);

    // Enhanced navigation may keep the element but drop the editor Quill added inside it.
    const remountEditors = () => document.querySelectorAll("k-rich-text").forEach(editor => editor.mount());

    // ---------- Searchable select ----------
    // <k-combobox><select name="...">...</select></k-combobox>
    // The select stays the form field (hidden); a text box filters its options as you type,
    // ignoring accents and case ("ανιχν" finds "Ανιχνευτές"). Without JavaScript the select works as is.

    const fold = text => text.normalize("NFD").replace(/\p{M}/gu, "").toLocaleLowerCase("el");
    let comboboxCount = 0;

    class Combobox extends HTMLElement {
        connectedCallback() {
            this.mount();
        }

        mount() {
            const select = this.querySelector("select");
            if (!select || this.querySelector("input.k-combobox-input")) {
                return;
            }

            const id = `k-combobox-${++comboboxCount}`;
            const input = document.createElement("input");
            input.type = "text";
            input.className = `${select.className} k-combobox-input`;
            input.autocomplete = "off";
            input.spellcheck = false;
            input.setAttribute("role", "combobox");
            input.setAttribute("aria-autocomplete", "list");
            input.setAttribute("aria-expanded", "false");
            input.setAttribute("aria-controls", `${id}-list`);
            input.placeholder = this.getAttribute("placeholder") ?? "Πληκτρολογήστε για αναζήτηση";

            // The label points at the select; move its id to the text box so clicking the label focuses it.
            if (select.id) {
                input.id = select.id;
                select.removeAttribute("id");
            }

            const list = document.createElement("ul");
            list.id = `${id}-list`;
            list.className = "k-combobox-list";
            list.setAttribute("role", "listbox");
            list.hidden = true;

            select.hidden = true;
            select.tabIndex = -1;
            select.after(input, list);

            const options = () => [...select.options];
            const labelOf = option => option?.textContent.trim() ?? "";
            let active = -1;

            const showSelected = () => {
                input.value = select.value ? labelOf(select.selectedOptions[0]) : "";
            };

            const close = () => {
                list.hidden = true;
                input.setAttribute("aria-expanded", "false");
                input.removeAttribute("aria-activedescendant");
                active = -1;
            };

            const render = () => {
                const query = fold(input.value.trim());
                list.replaceChildren();
                const matches = options().filter(o => !query || fold(o.textContent).includes(query));
                if (matches.length === 0) {
                    const empty = document.createElement("li");
                    empty.className = "k-combobox-empty";
                    empty.textContent = "Δεν βρέθηκε.";
                    list.append(empty);
                }
                matches.forEach((option, index) => {
                    const item = document.createElement("li");
                    item.id = `${id}-option-${index}`;
                    item.setAttribute("role", "option");
                    item.dataset.value = option.value;
                    item.textContent = labelOf(option);
                    item.setAttribute("aria-selected", String(option.value === select.value));
                    // mousedown, not click: it fires before the text box loses focus.
                    item.addEventListener("mousedown", event => {
                        event.preventDefault();
                        choose(option.value);
                    });
                    list.append(item);
                });
                active = -1;
                list.hidden = false;
                input.setAttribute("aria-expanded", "true");
            };

            const choose = value => {
                select.value = value;
                select.dispatchEvent(new Event("change", { bubbles: true }));
                showSelected();
                close();
            };

            const highlight = index => {
                const items = [...list.querySelectorAll("[role=option]")];
                if (items.length === 0) {
                    return;
                }
                active = (index + items.length) % items.length;
                items.forEach((item, i) => item.classList.toggle("active", i === active));
                input.setAttribute("aria-activedescendant", items[active].id);
                items[active].scrollIntoView({ block: "nearest" });
            };

            input.addEventListener("focus", () => {
                input.select();
                render();
            });
            input.addEventListener("input", render);
            input.addEventListener("keydown", event => {
                switch (event.key) {
                    case "ArrowDown":
                        event.preventDefault();
                        if (list.hidden) {
                            render();
                        }
                        highlight(active + 1);
                        break;
                    case "ArrowUp":
                        event.preventDefault();
                        highlight(active - 1);
                        break;
                    case "Enter":
                        if (!list.hidden && active >= 0) {
                            event.preventDefault();
                            choose(list.querySelectorAll("[role=option]")[active].dataset.value);
                        }
                        break;
                    case "Escape":
                        if (!list.hidden) {
                            event.preventDefault();
                            showSelected();
                            close();
                        }
                        break;
                }
            });
            // Leaving the box keeps the chosen option; half-typed text is dropped.
            input.addEventListener("blur", () => {
                showSelected();
                close();
            });

            showSelected();
        }
    }

    customElements.define("k-combobox", Combobox);

    // ---------- Gallery ----------
    // <a href="full.webp" data-gallery="team"><img src="thumb_full.webp"></a>
    // No data-title/data-description: GLightbox inserts them as HTML, and file names come from users.

    let lightbox = null;

    const mountGallery = async () => {
        lightbox?.destroy();
        lightbox = null;

        if (!document.querySelector("a[data-gallery]")) {
            return;
        }

        try {
            await loadScript("/lib/glightbox/glightbox.min.js");
        } catch {
            return; // The links still open the full image.
        }

        lightbox = GLightbox({ selector: "a[data-gallery]", loop: true, touchNavigation: true });
    };

    // ---------- Sortable tables ----------
    // table.k-sortable: each header becomes a button that sorts the rows (asc, then desc). A cell's
    // data-sort-value wins over its text (dates, sizes, game numbers); numbers sort as numbers, text in
    // Greek order ignoring accents. Headers with data-no-sort (actions) stay as they are. Sorts the rows
    // on the page only.
    const collator = new Intl.Collator("el", { sensitivity: "base", numeric: true });

    const sortValue = cell => cell?.dataset.sortValue ?? cell?.textContent.trim() ?? "";

    const compare = (a, b) => {
        const x = Number(a), y = Number(b);
        if (a !== "" && b !== "" && !Number.isNaN(x) && !Number.isNaN(y)) {
            return x - y;
        }

        return collator.compare(a, b);
    };

    const mountTable = table => {
        if (table.dataset.sortReady) {
            return;
        }

        table.dataset.sortReady = "true";
        const headers = [...table.tHead.rows[0].cells];

        headers.forEach((th, column) => {
            if (th.hasAttribute("data-no-sort") || !th.textContent.trim()) {
                return;
            }

            const button = document.createElement("button");
            button.type = "button";
            button.className = "k-sort";
            button.append(...th.childNodes);
            th.append(button);

            button.addEventListener("click", () => {
                const ascending = th.getAttribute("aria-sort") !== "ascending";
                headers.forEach(other => other.removeAttribute("aria-sort"));
                th.setAttribute("aria-sort", ascending ? "ascending" : "descending");

                const body = table.tBodies[0];
                const rows = [...body.rows];
                rows.sort((r1, r2) => compare(sortValue(r1.cells[column]), sortValue(r2.cells[column])) * (ascending ? 1 : -1));
                body.append(...rows);
            });
        });
    };

    const mount = () => {
        remountEditors();
        mountGallery();
        document.querySelectorAll("k-combobox").forEach(combobox => combobox.mount());
        document.querySelectorAll("table.k-sortable").forEach(mountTable);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", mount);
    } else {
        mount();
    }

    document.addEventListener("DOMContentLoaded", () => window.Blazor?.addEventListener("enhancedload", mount));
})();
