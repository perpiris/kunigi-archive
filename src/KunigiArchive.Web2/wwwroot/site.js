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

    const mount = () => {
        remountEditors();
        mountGallery();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", mount);
    } else {
        mount();
    }

    document.addEventListener("DOMContentLoaded", () => window.Blazor?.addEventListener("enhancedload", mount));
})();
