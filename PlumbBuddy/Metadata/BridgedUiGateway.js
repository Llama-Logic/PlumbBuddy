(function () {
    const sendMessageToPlumbBuddy = window.chrome?.webview
        ? message => {
            console.debug('To PB: %o', message);
            window.chrome.webview.postMessage(JSON.stringify(message));
        }
        : message => {
            console.debug('To PB: %o', message);
            window.webkit.messageHandlers.plumbBuddy.postMessage(JSON.stringify(message));
        };

    const gateway = {
        onMessageFromPlumbBuddy: function (message) {
            console.debug('From PB: %o', message);
        },

        testComms: function () {
            sendMessageToPlumbBuddy({
                debugMessage: 'Marco.',
            });
        },
    };

    if (window.chrome?.webview) {
        window.chrome.webview.addEventListener('message', e =>  gateway.onMessageFromPlumbBuddy(e.data));
    }

    Object.freeze(gateway);
    Object.defineProperty(window, 'gateway', {
        value: gateway,
        writable: false,
        configurable: false,
        enumerable: true,
    });

    console.info('PlumbBuddy %s Gateway loaded successfully. Use window.gateway to communicate with mods and services.', '__PB_VERSION__');
}());