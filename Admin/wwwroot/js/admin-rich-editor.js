(() => {
    "use strict";
    const $ = window.jQuery;
    if (!$ || !$.fn?.summernote) return;
    const settings = {
        lang: "fa-IR",
        height: 190,
        direction: "rtl",
        dialogsInBody: true,
        placeholder: "متن را اینجا بنویسید…",
        toolbar: [
            ["style", ["style"]],
            ["font", ["bold", "italic", "underline", "clear"]],
            ["para", ["ul", "ol", "paragraph"]],
            ["insert", ["link"]],
            ["view", ["fullscreen", "help"]]
        ],
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
