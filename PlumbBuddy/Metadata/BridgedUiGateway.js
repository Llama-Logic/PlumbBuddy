(function () {
    const sanitizeUuid = uuid => String(uuid).replace(/[^\da-f]/g, '').toLowerCase();
    const myUniqueId = sanitizeUuid('__UNIQUE_ID__');
    const sendMessageToPlumbBuddy = window.chrome?.webview
        ? message => window.chrome.webview.postMessage(JSON.stringify(message))
        : message => window.webkit.messageHandlers.plumbBuddy.postMessage(JSON.stringify(message));

    class Event {
        #listeners = [];

        constructor(receiveDispatch) {
            receiveDispatch(data => {
                this.#listeners.forEach(listener => {
                    try {
                        listener(data);
                    } catch (ex) {
                        console.error('Naughty bridged UI threw %o while dispatching event', ex);
                    }
                })
            });
        }

        addListener(listener) {
            this.#listeners.push(listener);
        }

        removeListener(listener) {
            const index = this.#listeners.indexOf(listener);
            if (index < 0) {
                return false;
            }
            this.#listeners.splice(index, 1);
            return true;
        }
    }

    function deepFreeze(obj) {
        Object.freeze(obj);
        for (const key of Object.getOwnPropertyNames(obj)) {
            const value = obj[key];
            if (value !== null && typeof value === 'object' && !Object.isFrozen(value)) {
                deepFreeze(value);
            }
        }
        return obj;
    }

    function makePromise() {
        let makingPromise = {};
        makingPromise.promise = new Promise((resolve, reject) => {
            makingPromise.resolve = resolve;
            makingPromise.reject = reject;
        });
        return makingPromise;
    }

    class ScriptModNotFoundError extends Error {
        constructor() {
            super('The script mod specified could not be found');
            this.name = 'ScriptModNotFoundError';
        }
    }

    class IndexNotFoundError extends Error {
        constructor() {
            super('The required index.html file could not be found in that script mod at the specified UI root location');
            this.name = 'IndexNotFoundError';
        }
    }

    class PlayerDeniedRequestError extends Error {
        constructor() {
            super('The player has denied your request to display that bridged UI');
            this.name = 'PlayerDeniedRequestError';
        }
    }

    class BridgedUiNotFoundError extends Error {
        constructor() {
            super('The referenced bridged UI is not currently loaded');
            this.name = 'BridgedUiNotFoundError';
        }
    }

    class BridgedUi {
        #announcement;
        #destroyed;
        #uniqueId;

        constructor(uniqueId, receiveDispatces) {
            this.#uniqueId = uniqueId;
            const dispatches = {};
            this.#announcement = new Event(dispatch => dispatches.announcement = dispatch);
            this.#destroyed = new Event(dispatch => dispatches.destroyed = dispatch)
            receiveDispatces(dispatches);
        }

        get announcement() {
            return this.#announcement;
        }

        get destroyed() {
            return this.#destroyed;
        }

        close() {
            sendMessageToPlumbBuddy({
                type: 'closeBridgedUi',
                uniqueId: this.#uniqueId,
            })
        }

        focus() {
            sendMessageToPlumbBuddy({
                type: 'focusBridgedUi',
                uniqueId: this.#uniqueId,
            })
        }

        sendData(data) {
            sendMessageToPlumbBuddy({
                type: 'sendDataToBridgedUi',
                recipient: this.#uniqueId,
                data
            })
        }
    }

    class RelationalDataStorageQueryRecordSet {
        #fieldNames;
        #records;

        constructor(recordSetMessageExcerpt) {
            this.#fieldNames = deepFreeze(recordSetMessageExcerpt.fieldNames);
            this.#records = deepFreeze(recordSetMessageExcerpt.records);
        }
    }

    const bridgedUis = [];
    const promisedBridgedUis = {};
    const promisedBridgedUiLookUps = {};
    let dispatchDataReceived = null;

    class Gateway {
        #dataReceived;
        #uniqueId = sanitizeUuid('__UNIQUE_ID__');
        #version = '__PB_VERSION__';

        constructor() {
            if (dispatchDataReceived) {
                throw new Error('Gateway has already been initialized for you. Use window.gateway.')
            }
            this.#dataReceived = new Event(dispatch => dispatchDataReceived = dispatch);
        }

        get dataReceived() {
            return this.#dataReceived;
        }

        get uniqueId() {
            return this.#uniqueId;
        }

        get version() {
            return this.#version;
        }

        /**
         * Makes an announcement to all script mods and other components with a reference to this bridged UI
         * @param {*} announcement the data to announce
         */
        announce(announcement) {
            sendMessageToPlumbBuddy({
                type: 'announcement',
                announcement
            });
        }

        /**
         * Attempts to look up a loaded bridged UI
         * @param {String} uniqueId the UUID for the tab of the bridged UI
         * @returns {Promise} an promise that will resolve with the bridged UI or a fault that it is not currently loaded
         */
        lookUpBridgedUi(uniqueId) {
            uniqueId = sanitizeUuid(uniqueId);
            if (!uniqueId) {
                throw new Error('uniqueId is not optional');
            }
            const alreadyLoaded = bridgedUis[uniqueId];
            if (alreadyLoaded) {
                return new Promise(resolve => resolve(alreadyLoaded.bridgedUi));
            }
            const alreadyPomised = promisedBridgedUiLookUps[uniqueId];
            if (alreadyPomised) {
                return alreadyPomised.promise;
            }
            const madePromise = makePromise();
            promisedBridgedUiLookUps[uniqueId] = madePromise;
            sendMessageToPlumbBuddy({
                type: 'bridgedUiLookUp',
                uniqueId,
            });
            return madePromise.promise;
        }

        onMessageFromPlumbBuddy(message) {
            if (message.type === 'bridgedUiAnnouncement') {
                const uniqueId = sanitizeUuid(message.uniqueId);
                const bridgedUi = bridgedUis[uniqueId];
                if (!bridgedUi) {
                    return;
                }
                bridgedUi.dispatches.announcement(message.announcement);
            } else if (message.type === 'bridgedUiData') {
                dispatchDataReceived(message.data);
            } else if (message.type === 'bridgedUiDestroyed') {
                const uniqueId = sanitizeUuid(message.uniqueId);
                const bridgedUi = bridgedUis[uniqueId];
                if (bridgedUi) {
                    delete bridgedUis[uniqueId];
                    bridgedUi.dispatches.destroyed();
                }
            } else if (message.type === 'bridgedUiLookUpResponse') {
                const uniqueId = sanitizeUuid(message.uniqueId);
                let bridgedUi;
                const { isLoaded } = message;
                if (isLoaded) {
                    bridgedUi = bridgedUis[uniqueId];
                    if (!bridgedUi) {
                        let dispatches;
                        bridgedUi = new BridgedUi(uniqueId, receivedDispatches => dispatches = receivedDispatches);
                        bridgedUis[uniqueId] = {
                            bridgedUi,
                            dispatches
                        };
                    } else {
                        bridgedUi = bridgedUi.bridgedUi;
                    }
                }
                const promisedBridgedUiLookUp = promisedBridgedUiLookUps[uniqueId];
                if (!promisedBridgedUiLookUp) {
                    return;
                }
                delete promisedBridgedUiLookUps[uniqueId];
                if (bridgedUi) {
                    promisedBridgedUiLookUp.resolve(bridgedUi);
                    return;
                }
                promisedBridgedUiLookUp.reject(new BridgedUiNotFoundError());
            } else if (message.type === 'bridgedUiRequestResponse') {
                const uniqueId = sanitizeUuid(message.uniqueId);
                let bridgedUi;
                const { denialReason } = message;
                if (!denialReason) {
                    bridgedUi = bridgedUis[uniqueId];
                    if (!bridgedUi) {
                        let dispatches;
                        bridgedUi = new BridgedUi(uniqueId, receivedDispatches => dispatches = receivedDispatches);
                        bridgedUis[uniqueId] = {
                            bridgedUi,
                            dispatches
                        };
                    } else {
                        bridgedUi = bridgedUi.bridgedUi;
                    }
                }
                const promisedBridgedUi = promisedBridgedUis[uniqueId];
                if (!promisedBridgedUi) {
                    return;
                }
                delete promisedBridgedUis[uniqueId];
                if (bridgedUi) {
                    promisedBridgedUi.resolve(bridgedUi);
                    return;
                }
                let fault;
                if (denialReason === 1) {
                    fault = new ScriptModNotFoundError();
                } else if (denialReason === 2) {
                    fault = new IndexNotFoundError();
                } else if (denialReason === 3) {
                    fault = new PlayerDeniedRequestError();
                } else {
                    fault = new Error('Unknown denial reason')
                }
                promisedBridgedUi.reject(fault);
            }
        }

        /**
         * Requests a bridged UI from PlumbBuddy
         * @param {String} scriptMod either a Mods folder relative path to the `.ts4script` file containing the bridged UI's files *-or-* the hex of the SHA 256 calculated hash of the `.ts4script` file if it is manifested
         * @param {String} uiRoot the path inside the `.ts4script` file to the root of the bridged UI's files (this is where `index.html` should be located)
         * @param {String} uniqueId the UUID you are assigning to this tab to identify it to other gateway participants
         * @param {String} requestorName the name of party making the request, to be presented to the player
         * @param {String} requestReason the reason the party is making the request, to be presented to the player
         * @param {String} tabName the name of the tab for the bridged UI in PlumbBuddy's interface if the request is approved
         * @param {String?} tabIconPath a path to an icon to be displayed on the bridged UI's tab in PlumbBuddy's interface, inside the `.ts4script` file, relative to `ui_root`
         * @returns {Promise} an promise that will resolve with the bridged UI or a fault indicating why your request was denied (e.g. `ScriptModNotFoundError`, `IndexNotFoundError`, `PlayerDeniedRequestError`, etc.)
         */
        requestBridgedUi(scriptMod, uiRoot, uniqueId, requestorName, requestReason, tabName, tabIconPath) {
            uniqueId = sanitizeUuid(uniqueId);
            if (!uiRoot) {
                throw new Error('uiRoot is not optional');
            }
            if (!uniqueId) {
                throw new Error('uniqueId is not optional');
            }
            if (!requestorName) {
                throw new Error('requestorName is not optional');
            }
            if (!requestReason) {
                throw new Error('requestReason is not optional');
            }
            if (!tabName) {
                throw new Error('tabName is not optional');
            }
            const alreadyLoaded = bridgedUis[uniqueId];
            if (alreadyLoaded) {
                return new Promise(resolve => resolve(alreadyLoaded.bridgedUi));
            }
            const alreadyPomised = promisedBridgedUis[uniqueId];
            if (alreadyPomised) {
                return alreadyPomised.promise;
            }
            const madePromise = makePromise();
            promisedBridgedUis[uniqueId] = madePromise;
            sendMessageToPlumbBuddy({
                type: 'bridgedUiRequest',
                scriptMod,
                uiRoot,
                uniqueId,
                requestorName,
                requestReason,
                tabName,
                tabIconPath,
            });
            return madePromise.promise;
        }
    }

    const gateway = new Gateway();

    if (window.chrome?.webview) {
        window.chrome.webview.addEventListener('message', e => gateway.onMessageFromPlumbBuddy(e.data));
    }

    Object.freeze(gateway);
    Object.defineProperty(window, 'gateway', {
        value: gateway,
        writable: false,
        configurable: false,
        enumerable: true,
    });

    console.info('PlumbBuddy %s Gateway loaded successfully. Use window.gateway to communicate with mods and services. This is bridged UI "%s".', gateway.version, gateway.uniqueId);
}());