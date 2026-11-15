(() => {
    "use strict";
    const $ = window.jQuery;
    if (!$ || !$.fn?.summernote) return;
    const emojis = ["🙂", "😀", "😊", "😂", "😍", "😉", "😎", "🤔", "😢", "🙏", "👏", "👍", "👎", "❤️", "💙", "✨", "⭐", "🔥", "🎉", "✅", "❌", "⚠️", "📌", "📦", "🛒", "💡", "🔧", "⚡", "🧰", "📞", "📧", "🚚"];

    function createEmojiButton(context) {
        let picker;
        let dismissPicker;
        let activeGroup;

        function closePicker() {
            picker?.remove();
            picker = null;
            activeGroup?.classList.remove("admin-rich-editor-has-picker");
            activeGroup = null;
            if (dismissPicker) {
                document.removeEventListener("pointerdown", dismissPicker);
                document.removeEventListener("keydown", dismissPicker);
                dismissPicker = null;
            }
        }

        return $.summernote.ui.button({
            className: "admin-rich-editor-emoji-button",
            contents: "😀",
            tooltip: "افزودن ایموجی",
            click(event) {
                event.preventDefault();
                event.stopPropagation();
                if (picker) {
                    closePicker();
                    return;
                }

                picker = document.createElement("div");
                picker.className = "admin-rich-editor-emoji-picker";
                picker.setAttribute("role", "group");
                picker.setAttribute("aria-label", "انتخاب ایموجی");
                for (const emoji of emojis) {
                    const option = document.createElement("button");
                    option.type = "button";
                    option.className = "admin-rich-editor-emoji-option";
                    option.textContent = emoji;
                    option.setAttribute("aria-label", emoji);
                    option.title = emoji;
                    option.addEventListener("click", () => {
                        context.invoke("editor.insertText", emoji);
                        closePicker();
                    });
                    picker.append(option);
                }

                activeGroup = event.currentTarget.closest(".note-btn-group");
                activeGroup?.classList.add("admin-rich-editor-has-picker");
                activeGroup?.append(picker);
                dismissPicker = keyEvent => {
                    if (keyEvent.type === "keydown" && keyEvent.key !== "Escape") return;
                    if (keyEvent.type === "pointerdown" && picker?.contains(keyEvent.target)) return;
                    closePicker();
                };
                setTimeout(() => {
                    if (picker) {
                        document.addEventListener("pointerdown", dismissPicker);
                        document.addEventListener("keydown", dismissPicker);
                    }
                }, 0);
            }
        }).render();
    }

    const settings = {
        lang: "fa-IR",
        height: 190,
        direction: "rtl",
        dialogsInBody: true,
        placeholder: "متن را اینجا بنویسید…",
        toolbar: [
            ["style", ["style"]],
            ["font", ["bold", "italic", "underline", "strikethrough", "clear"]],
            ["para", ["ul", "ol", "paragraph"]],
            ["insert", ["link", "emoji"]],
            ["view", ["fullscreen", "help"]]
        ],
        buttons: { emoji: createEmojiButton },
        styleTags: ["p", "blockquote", "h3", "h4"],
        callbacks: { onChange(contents) { this.value = contents; } }
    };
    function initialize(root = document) {
        root.querySelectorAll?.("textarea[data-rich-editor]").forEach(textarea => {
            if (textarea.dataset.richEditorReady === "true") return;
            textarea.dataset.richEditorReady = "true";
            $(textarea).summernote(settings);
        });
    }
    window.AdminRichEditor = Object.freeze({
        initialize,
        getValue(textarea) { return $(textarea).summernote("code"); },
        setValue(textarea, value) { $(textarea).summernote("code", value || ""); }
    });
    initialize();
    new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => initialize(node.nodeType === 1 ? node : document))))
        .observe(document.body, { childList: true, subtree: true });
})();
