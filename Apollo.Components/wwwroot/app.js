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