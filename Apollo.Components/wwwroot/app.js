export function captureConsoleOutput(dotnetHelper) {
    // Save the original console methods
    const originalLog = console.log;
    const originalError = console.error;
    const originalWarn = console.warn;

    // Override console.log
    console.log = function (...args) {
        const message = args.map(arg => (typeof arg === 'object' ? JSON.stringify(arg) : arg)).join(' ');
        dotnetHelper.invokeMethodAsync("OnConsoleLog", message);
        originalLog.apply(console, args); // Call the original method
    };

    // Override console.error
    console.error = function (...args) {
        const message = args.map(arg => (typeof arg === 'object' ? JSON.stringify(arg) : arg)).join(' ');
        dotnetHelper.invokeMethodAsync("OnConsoleError", message);
        originalError.apply(console, args);
    };

    // Override console.warn
    console.warn = function (...args) {
        const message = args.map(arg => (typeof arg === 'object' ? JSON.stringify(arg) : arg)).join(' ');
        dotnetHelper.invokeMethodAsync("OnConsoleWarn", message);
        originalWarn.apply(console, args);
    };
}

export function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

export async function readFromClipboard() {
    try {
        return await navigator.clipboard.readText();
    } catch (err) {
        console.error('Failed to read clipboard:', err);
        return null;
    }
}

export async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy to clipboard:', err);
        return false;
    }
}

window.updateHtmlPreview = (element, html) => {
    if (!element) return;

    // Sanitize the HTML before rendering
    /*
    const sanitizedHtml = DOMPurify.sanitize(html, {
        ALLOWED_TAGS: ['div', 'span', 'p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'a', 'img', 'ul', 'ol', 'li', 'table', 'tr', 'td', 'th', 'thead', 'tbody', 'br', 'hr', 'strong', 'em', 'code', 'pre'],
        ALLOWED_ATTR: ['href', 'src', 'alt', 'class', 'style', 'target']
    });
    */

    element.innerHTML = html;
};