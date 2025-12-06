(function () {
    const sendMessageToPlumbBuddy = window.chrome?.webview
        ? message => window.chrome.webview.postMessage(JSON.stringify(message))
        : message => window.webkit.messageHandlers.plumbBuddy.postMessage(JSON.stringify(message));

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

    function generateUUIDv4() {
        const bytes = crypto.getRandomValues(new Uint8Array(16));

        // Per RFC4122 standard
        bytes[6] = (bytes[6] & 0x0f) | 0x40; // Version 4
        bytes[8] = (bytes[8] & 0x3f) | 0x80; // Variant 10xxxxxx

        return sanitizeUuid([...bytes].map(b => b.toString(16).padStart(2, '0')).join(''));
    }

    function isValidUuid(uuid) {
        return /^[\da-f]{8}\-([\da-f]{4}\-){3}[\da-f]{12}$/i.test(String(uuid));
    }
    
    function makePromise() {
        let makingPromise = {};
        makingPromise.promise = new Promise((resolve, reject) => {
            makingPromise.resolve = resolve;
            makingPromise.reject = reject;
        });
        return makingPromise;
    }

    function sanitizeUuid(uuid) {
        uuid = String(uuid).replace(/[^\da-f]/g, '').toLowerCase();
        if (uuid.length !== 32) {
            return null;
        }
        return [
            uuid.slice(0, 8),
            uuid.slice(8, 12),
            uuid.slice(12, 16),
            uuid.slice(16, 20),
            uuid.slice(20)
        ].join('-');
    }

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
            if (!listener) {
                throw new Error('listener is not optional');
            }
            if (typeof listener !== 'function' && !(listener instanceof Function)) {
                throw new Error('listener must be callable');
            }
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

    class InvalidHostNameError extends Error {
        constructor() {
            super('The host name you specified is not legal by the DNS standard - use only letters from the Latin alphabet, Arabic numerals, and the dash (-)');
            this.name = 'InvalidHostNameError';
        }
    }

    class BridgedUiNotFoundError extends Error {
        constructor() {
            super('The referenced bridged UI is not currently loaded');
            this.name = 'BridgedUiNotFoundError';
        }
    }

    /**
     * A PlumbBuddy Runtime Mod Integration Bridged UI
     */
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

        /**
         * Gets the event which is dispatched when this bridged UI makes an announcement; event data will be what was announced
         * @returns {Event}
         */
        get announcement() {
            return this.#announcement;
        }

        /**
         * Gets the event which is dispatched when this bridged UI has been destroyed for any reason
         * @returns {Event}
         */
        get destroyed() {
            return this.#destroyed;
        }

        /**
         * Closes the bridged UI
         */
        close() {
            sendMessageToPlumbBuddy({
                type: 'closeBridgedUi',
                uniqueId: this.#uniqueId,
            });
        }

        /**
         * Sends data to the bridged UI
         * @param {*} data the data to be send
         */
        sendData(data) {
            sendMessageToPlumbBuddy({
                type: 'sendDataToBridgedUi',
                recipient: this.#uniqueId,
                data,
            });
        }
    }

    /**
     * A PlumbBuddy Runtime Mod Integration Gamepad Button
     */
    class GamepadButton {
        #changed;
        #gamepad;
        #name;
        #pressed;

        constructor(gamepad, name, pressed, receiveChanger) {
            this.#gamepad = gamepad;
            this.#name = name;
            this.#pressed = pressed;
            const dispatches = {};
            this.#changed = new Event(dispatch => dispatches.changed = dispatch);
            receiveChanger(pressed => {
                this.#pressed = pressed;
                dispatches.changed(this);
            });
        }

        /**
         * Gets the event which is dispatched when the state of the button has changed
         * @returns {Event}
         */
        get changed() {
            return this.#changed;
        }

        /**
         * Gets the gamepad to which this button belongs
         * @returns {Gamepad}
         */
        get gamepad() {
            return this.#gamepad;
        }

        /**
         * Gets the name of the button
         * @returns {String}
         */
        get name() {
            return this.#name;
        }

        /**
         * Gets whether the button is pressed
         * @returns {Boolean}
         */
        get pressed() {
            return this.#pressed;
        }
    }

    /**
     * A PlumbBuddy Runtime Mod Integration Gamepad Thumbstick
     */
    class GamepadThumbstick {
        #changed;
        #direction;
        #gamepad;
        #index;
        #position;
        #x;
        #y;

        constructor(gamepad, index, x, y, direction, position, receiveChanger) {
            this.#gamepad = gamepad;
            this.#index = index;
            this.#x = x;
            this.#y = y;
            this.#direction = direction;
            this.#position = position;
            const dispatches = {};
            this.#changed = new Event(dispatch => dispatches.changed = dispatch);
            receiveChanger((x, y, direction, position) => {
                this.#x = x;
                this.#y = y;
                this.#direction = direction;
                this.#position = position;
                dispatches.changed(this);
            });
        }

        /**
         * Gets the event which is dispatched when the state of the thumbstick has changed
         * @returns {Event}
         */
        get changed() {
            return this.#changed;
        }

        /**
         * Gets the direction in which the thumbstick is pointed as a float (East being 0, South being π/2, West being π or -π, North being -π/2)
         * @returns {Number}
         */
        get direction() {
            return this.#direction;
        }

        /**
         * Gets the gamepad to which this thumbstick belongs
         * @returns {Gamepad}
         */
        get gamepad() {
            return this.#gamepad;
        }

        /**
         * Gets the index of the thumbstick according to the gamepad to which it belongs
         * @returns {Number}
         */
        get index() {
            return this.#index;
        }

        /**
         * Gets the position of the thumbstick as a float inclusively between 0 (dead center) and 1 (as far in the direction as is possible)
         * [Important: When pushed all the way in one direction, a thumbstick may report its position as slightly greater than 1]
         * @returns {Number}
         */
        get position() {
            return this.#position;
        }

        /**
         * Gets the X axis coordinate of the thumbstick as a float inclusively between -1 (all the way West) and 1 (all the way East)
         * @returns {Number}
         */
        get x() {
            return this.#x;
        }

        /**
         * Gets the Y axis coordinate of the thumbstick as a float inclusively between -1 (all the way North) and 1 (all the way South)
         * @returns {Number}
         */
        get y() {
            return this.#y;
        }
    }

    /**
     * A PlumbBuddy Runtime Mod Integration Gamepad Trigger
     */
    class GamepadTrigger {
        #changed;
        #gamepad;
        #index;
        #position;

        constructor(gamepad, index, position, receiveChanger) {
            this.#gamepad = gamepad;
            this.#index = index;
            this.#position = position;
            const dispatches = {};
            this.#changed = new Event(dispatch => dispatches.changed = dispatch);
            receiveChanger(position => {
                this.#position = position;
                dispatches.changed(this);
            });
        }

        /**
         * Gets the event which is dispatched when the state of the trigger has changed
         * @returns {Event}
         */
        get changed() {
            return this.#changed;
        }

        /**
         * Gets the gamepad to which this trigger belongs
         * @returns {Gamepad}
         */
        get gamepad() {
            return this.#gamepad;
        }

        /**
         * Gets the index of the trigger according to the gamepad to which it belongs
         * @returns {Number}
         */
        get index() {
            return this.#index;
        }

        /**
         * Gets the position of the trigger as a float inclusively between 0 (no pressure) and 1 (fully squeezed)
         * @returns {Number}
         */
        get position() {
            return this.#position;
        }
    }

    /**
     * A PlumbBuddy Runtime Mod Integration Gamepad
     */
    class Gamepad {
        #buttons;
        #disconnected;
        #index;
        #indexChanged;
        #name;
        #thumbsticks;
        #triggers;

        constructor(index, name, buttons, thumbsticks, triggers, receiveChangers) {
            this.#index = index;
            this.#name = name;
            const buttonChangers = {};
            this.#buttons = Object.fromEntries(buttons.map(button => {
                const name = button[0];
                return [
                    name,
                    new GamepadButton(this, name, button[1], changer => buttonChangers[name] = changer)
                ];
            }));
            const thumbstickChangers = [];
            this.#thumbsticks = thumbsticks.map((thumbstick, index) => {
                return new GamepadThumbstick(this, index, thumbstick[0], thumbstick[1], thumbstick[2], thumbstick[3], changer => thumbstickChangers.push(changer));
            });
            const triggerChangers = [];
            this.#triggers = triggers.map((position, index) => {
                return new GamepadTrigger(this, index, position, changer => triggerChangers.push(changer));
            });
            const dispatches = {};
            this.#disconnected = new Event(dispatch => dispatches.disconnected = dispatch);
            this.#indexChanged = new Event(dispatch => dispatches.indexChanged = dispatch);
            receiveChangers({
                buttonChangers,
                disconnect: () => dispatches.disconnected(this),
                indexChanger: index => {
                    if (this.#index !== index) {
                        this.#index = index;
                        dispatches.indexChanged(this);
                    }
                },
                thumbstickChangers,
                triggerChangers,
            });
        }

        /**
         * Gets the names of the gamepad's buttons
         * @returns {Array<String>}
         */
        get buttonNames() {
            return Object.keys(this.#buttons);
        }

        /**
         * Gets the event which is dispatched when the gamepad has been disconnected
         * @returns {Event}
         */
        get disconnected() {
            return this.#disconnected;
        }

        /**
         * Gets the index of this gamepad according to the gateway
         * @returns {Number}
         */
        get index() {
            return this.#index;
        }

        /**
         * Gets the event which is dispatched when the gamepad's index according to the gateway has changed
         * @returns {Event}
         */
        get indexChanged() {
            return this.#indexChanged;
        }

        /**
         * Gets the name of the gamepad as reported by the host operating system
         * @returns {String}
         */
        get name() {
            return this.#name;
        }

        /**
         * Gets the number of thumbsticks the gamepad has
         * @returns {Number}
         */
        get thumbstickCount() {
            return this.#thumbsticks.length;
        }

        /**
         * Gets the number of triggers the gamepad has
         * @returns {Number}
         */
        get triggerCount() {
            return this.#triggers.length;
        }

        /**
         * Gets the gamepad's button with the specified name
         * @param {String} name the name of the button
         * @returns {GamepadButton}
         */
        getButton(name) {
            return this.#buttons[name];
        }

        /**
         * Gets the gamepad's thumbstick with the specified index
         * @param {Number} index the index of the thumbstick
         * @returns {GamepadThumbstick}
         */
        getThumbstick(index) {
            return this.#thumbsticks[index];
        }

        /**
         * Gets the gamepad's trigger with the specified index
         * @param {Number} index the index of the trigger
         * @returns {GamepadTrigger}
         */
        getTrigger(index) {
            return this.#triggers[index];
        }

        /**
         * Vibrates the controller with the specified intensity
         * @param {Number} intensity an intensity value between 0 (off) and 1 (full intensity)
         * @param {Number} duration the number of seconds to vibrate the gamepad (up to 30 seconds)
         */
        vibrate(intensity, duration) {
            sendMessageToPlumbBuddy({
                type: 'vibrateGamepad',
                index: this.#index,
                intensity: intensity,
                duration: duration,
            });
        }
    }

    /**
     * A PlumbBuddy Runtime Mod Integration Relational Data Storage Query Record Set
     */
    class RelationalDataStorageQueryRecordSet {
        #fieldNames;
        #records;

        constructor(recordSetMessageExcerpt) {
            this.#fieldNames = Object.freeze(recordSetMessageExcerpt.fieldNames);
            this.#records = deepFreeze(recordSetMessageExcerpt.records.map(record => record.map(value => {
                if (typeof value === 'object' || value instanceof Object) {
                    const base64 = value?.base64;
                    if (base64) {
                        const binaryString = atob(base64);
                        const ii = binaryString.length;
                        const binary = new Uint8Array(ii);
                        for (let i = 0; i < ii; ++i) {
                            binary[i] = binaryString.charCodeAt(i);
                        }
                        return binary;
                    }
                }
                return value;
            })));
        }

        /**
         * Gets the names of the fields of the record set
         * @returns {Array<String>}
         */
        get fieldNames() {
            return this.#fieldNames;
        }

        /**
         * Gets the records of the record set
         * @returns {Array<Array<Object>>}
         */
        get records() {
            return this.#records;
        }
    }

    /**
     * Data for the queryCompleted event of a PlumbBuddy Runtime Mod Integration Relational Data Storage Connection
     */
    class RelationalDataStorageQueryCompletedEventData {
        #errorCode;
        #errorMessage;
        #executionSeconds;
        #extendedErrorCode;
        #queryId;
        #recordSets;
        #tag;

        constructor(responseMessage) {
            this.#errorCode = responseMessage.errorCode;
            this.#errorMessage = responseMessage.errorMessage;
            this.#executionSeconds = responseMessage.executionSeconds;
            this.#extendedErrorCode = responseMessage.extendedErrorCode;
            this.#queryId = sanitizeUuid(responseMessage.queryId);
            this.#recordSets = Object.freeze(responseMessage.recordSets.map(recordSet => new RelationalDataStorageQueryRecordSet(recordSet)));
            this.#tag = responseMessage.tag;
        }

        /**
         * Gets the SQLite error code raised by the query (see: https://www.sqlite.org/rescode.html); -1 if another type of error occured; otherwise, 0
         * @returns {Number}
         */
        get errorCode() {
            return this.#errorCode;
        }

        /**
         * Gets the message of the error if one was raised; otherwise, None
         * @returns {String}
         */
        get errorMessage() {
            return this.#errorMessage;
        }

        /**
         * Gets the number of seconds for which the query was executing
         * @returns {Number}
         */
        get executionSeconds() {
            return this.#executionSeconds;
        }

        /**
         * Gets the SQLite extended error code raised by the query (see: https://www.sqlite.org/rescode.html#extrc); -1 if another type of error occured; otherwise, 0
         * @returns {Number}
         */
        get extendedErrorCode() {
            return this.#extendedErrorCode;
        }

        /**
         * Gets the UUID which was assigned to the query when execute was called on the connection
         * @returns {String}
         */
        get queryId() {
            return this.#queryId;
        }

        /**
         * Gets the record sets resulting from the query
         * @returns {Array<RelationalDataStorageQueryRecordSet>}
         */
        get recordSets() {
            return this.#recordSets;
        }

        /**
         * Gets the tag string if one was provided when execute was called on the connection; otherwise, None
         * @returns {String}
         */
        get tag() {
            return this.#tag;
        }
    }

    /**
     * A PlumbBuddy Runtime Mod Integration Relational Data Storage Connection
     */
    class RelationalDataStorage {
        #uniqueId;
        #isSaveSpecific;
        #queryCompleted;

        constructor(uniqueId, isSaveSpecific, receiveDispatches) {
            this.#uniqueId = uniqueId;
            this.#isSaveSpecific = isSaveSpecific;
            const dispatches = {};
            this.#queryCompleted = new Event(dispatch => dispatches.queryCompleted = dispatch);
            receiveDispatches(dispatches);
        }

        /**
         * Gets the UUID of the relational data
         * @returns {String}
         */
        get uniqueId() {
            return this.#uniqueId;
        }

        /**
         * Gets whether the relational data is specific to the currently open save file
         * @returns {Boolean}
         */
        get isSaveSpecific() {
            return this.#isSaveSpecific;
        }

        /**
         * Gets the event which is dispatched when a query for this connection has been completed
         * @returns {Event}
         */
        get queryCompleted() {
            return this.#queryCompleted;
        }

        /**
         * Executes a query with this connection
         * @param {String} sql SQLite query
         * @param {String} tag (optional) a tag to associate with the query, making its results easier to identify by other components
         * @param {Object} parameters the parameters of the query
         * @returns {String} the UUID which has been assigned to the query
         */
        execute(sql, tag, parameters) {
            if (!sql) {
                throw new Error('sql is not optional');
            }
            if (typeof sql !== 'string' && !(sql instanceof String)) {
                throw new Error('sql must be a string');
            }
            const queryId = generateUUIDv4();
            const serializationSafeParameters = {};
            if (parameters) {
                Object.keys(parameters).forEach(key => {
                    const value = parameters[key];
                    if (value instanceof Uint8Array) {
                        let binary = '';
                        for (let i = 0; i < value.length; ++i) {
                            binary += String.fromCharCode(value[i]);
                        }
                        serializationSafeParameters[key] = { base64: btoa(binary) };
                        return;
                    }
                    serializationSafeParameters[key] = value;
                });
            }
            sendMessageToPlumbBuddy({
                isSaveSpecific: this.#isSaveSpecific,
                parameters: serializationSafeParameters,
                query: sql,
                queryId,
                tag: tag ? String(tag) : null,
                type: 'queryRelationalDataStorage',
                uniqueId: this.#uniqueId,
            });
            return queryId;
        }
    }

    /**
     * An entry from a string table
     */
    class StringTableEntry {
        #locale;
        #locKey;
        #value;

        constructor(lookUpResponseMessageExcerpt) {
            this.#locale = lookUpResponseMessageExcerpt.locale;
            this.#locKey = lookUpResponseMessageExcerpt.locKey;
            this.#value = lookUpResponseMessageExcerpt.value;
        }

        /**
         * Gets the integer identifying the locale of the origin string table (hint: this is the first byte of the STBL's full instance)
         */
        get locale() {
            return this.#locale;
        }

        /**
         * Gets the LOCKEY for the entry (typically the FNV 32 hash of the value) expressed as an int
         */
        get locKey() {
            return this.#locKey;
        }

        /**
         * Gets the value of the entry (the actual text with no token replacements)
         */
        get value() {
            return this.#value;
        }
    }

    const bridgedUis = [];
    const gamepads = [];
    const gamepadChangers = [];
    const globalRelationalDataStores = [];
    const promisedBridgedUis = {};
    const promisedBridgedUiLookUps = {};
    let promisedScreenshotList = null;
    const promisedScreenshotDetails = {};
    const promisedStringTableEntriesLookUps = {};
    const saveSpecificRelationalDataStores = [];
    let dispatchDataReceived = null;
    let dispatchGamepadConnected = null;
    let dispatchScreenshotsChanged = null;

    /**
     * The PlumbBuddy Runtime Mod Integration Gateway
     */
    class Gateway {
        #dataReceived;
        #gamepadConnected;
        #screenshotsChanged;
        #uniqueId = sanitizeUuid('__UNIQUE_ID__');
        #version = '__PB_VERSION__';

        constructor() {
            if (dispatchDataReceived) {
                throw new Error('Gateway has already been initialized for you. Use window.gateway.')
            }
            this.#dataReceived = new Event(dispatch => dispatchDataReceived = dispatch);
            this.#gamepadConnected = new Event(dispatch => dispatchGamepadConnected = dispatch);
            this.#screenshotsChanged = new Event(dispatch => dispatchScreenshotsChanged = dispatch);
        }

        /**
         * Gets the event which is dispatched when this bridged UI has received data from a mod or another component
         * @returns {Event}
         */
        get dataReceived() {
            return this.#dataReceived;
        }

        /**
         * Gets the event which is dispatched when the player has connected a gamepad
         * @returns {Event}
         */
        get gamepadConnected() {
            return this.#gamepadConnected;
        }

        /**
         * Gets the number of currently connected gamepads
         * @returns {Number}
         */
        get gamepadCount() {
            return gamepads.length;
        }

        /**
         * Gets the event which is dispatched when the player's screenshots have changed
         * @returns {Event}
         */
        get screenshotsChanged() {
            return this.#screenshotsChanged;
        }

        /**
         * Gets the unique ID of this bridged UI
         * @returns {String}
         */
        get uniqueId() {
            return this.#uniqueId;
        }

        /**
         * Gets the version of PlumbBuddy
         * @returns {String}
         */
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
         * Causes PlumbBuddy to attempt bring the game to the foreground of the user's desktop, which will work if it, itself, in the foreground
         */
        foregroundGame() {
            sendMessageToPlumbBuddy({
                type: 'foregroundGame',
            });
        }

        /**
         * Gets the gamepad with the specified index
         * @param {Number} index the index of the gamepad to retrieve
         * @returns A gamepad if one is available at the specified index; otherwise, undefined
         */
        getGamepad(index) {
            return gamepads[index];
        }

        /**
         * Gets a Relational Data Storage connection
         * @param {String} uniqueId the UUID for the Relational Data Storage instance
         * @param {Boolean} isSaveSpecific True if the Relational Data Storage instance should be tied to the currently open save game; otherwise, False
         * @returns {RelationalDataStorage}
         */
        getRelationalDataStorage(uniqueId, isSaveSpecific) {
            uniqueId = sanitizeUuid(uniqueId);
            if (!isValidUuid(uniqueId)) {
                throw new Error('uniqueId is not optional and must be a valid UUID');
            }
            const dataStores = isSaveSpecific ? saveSpecificRelationalDataStores : globalRelationalDataStores;
            const alreadyLoaded = dataStores[uniqueId];
            if (alreadyLoaded) {
                return alreadyLoaded.dataStore;
            }
            let dispatches;
            const dataStore = new RelationalDataStorage(uniqueId, isSaveSpecific, receiveDispatces => dispatches = receiveDispatces);
            dataStores[uniqueId] = {
                dataStore,
                dispatches
            };
            return dataStore;
        }

        /**
         * Requests the details for the specified screenshot
         * @param {String} the name of the screenshot
         * @returns {Promise} a promise that will resolve with the details for the specified screenshot
         */
        getScreenshotDetails(screenshotName) {
            const alreadyPromised = promisedScreenshotDetails[screenshotName];
            if (alreadyPromised) {
                return alreadyPromised.promise;
            }
            const madePromise = makePromise();
            promisedScreenshotDetails[screenshotName] = madePromise;
            sendMessageToPlumbBuddy({
                type: 'getScreenshotDetails',
                name: screenshotName,
            });
            return madePromise.promise;
        }

        /**
         * Attempts to look up a loaded bridged UI
         * @param {String} uniqueId the UUID for the tab of the bridged UI
         * @returns {Promise} a promise that will resolve with the bridged UI or a fault that it is not currently loaded
         */
        lookUpBridgedUi(uniqueId) {
            uniqueId = sanitizeUuid(uniqueId);
            if (!isValidUuid(uniqueId)) {
                throw new Error('uniqueId is not optional and must be a valid UUID');
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

        /**
         * Requests a list of the screenshots currently in the player's Screenshots folder
         * @returns {Promise} a promise that will resolve with the list of screenshots currently in the player's Screenshots folder
         */
        listScreenshots() {
            if (promisedScreenshotList) {
                return promisedScreenshotList.promise;
            }
            const madePromise = makePromise();
            promisedScreenshotList = madePromise;
            sendMessageToPlumbBuddy({
                type: 'listScreenshots'
            });
            return promisedScreenshotList.promise;
        }

        /**
         * Requests string table entries which have been loaded by the game
         * @param {Array<Number>} locKeys a sequence of LOCKEYs for the entries desired, expressed as integers
         * @param {Array<Number>} locales (optional) a sequence of locales for the entries desired, expressed as integers (hint: these integers are the first byte of the STBL's full instance) -- if omitted, all locales will be included
         * @returns {Promise} a promise that will resolve when any entries matching the look up request have been found or a fault indicating why the look up failed
         */
        lookUpStringTableEntries(locKeys, locales) {
            const lookUpId = generateUUIDv4();
            const message = {
                locKeys,
                lookUpId,
                type: 'lookUpLocalizedStrings',
            };
            if (locales && Array.isArray(locales) && locales.length) {
                message.locales = locales;
            }
            const madePromise = makePromise();
            promisedStringTableEntriesLookUps[lookUpId] = madePromise;
            sendMessageToPlumbBuddy(message);
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
                } else if (denialReason === 4) {
                    fault = new InvalidHostNameError();
                } else {
                    fault = new Error('Unknown denial reason')
                }
                promisedBridgedUi.reject(fault);
            } else if (message.type === 'gamepadButtonChanged') {
                gamepadChangers[message.index].buttonChangers[message.name](message.pressed);
            } else if (message.type === 'gamepadConnected') {
                const gamepad = new Gamepad(message.index, message.name, message.buttons, message.thumbsticks, message.triggers, changers => gamepadChangers.splice(message.index, 0, changers));
                gamepads.splice(message.index, 0, gamepad);
                for (let i = message.index + 1; i < gamepadChangers.length; ++i) {
                    gamepadChangers[i].indexChanger(i);
                }
                dispatchGamepadConnected(gamepad);
            } else if (message.type === 'gamepadDisconnected') {
                gamepads.splice(message.index, 1);
                const removedChangers = gamepadChangers.splice(message.index, 1)[0];
                for (let i = message.index + 1; i < gamepadChangers.length; ++i) {
                    gamepadChangers[i].indexChanger(i);
                }
                removedChangers.disconnect();
            } else if (message.type === 'gamepadMoved') {
                gamepads.splice(message.newIndex, 0, gamepads.splice(message.index, 1));
                gamepadChangers.splice(message.newIndex, 0, gamepadChangers.splice(message.index, 1));
                for (let i = Math.min(message.index, message.newIndex); i < Math.max(message.index, message.newIndex) + 1; ++i) {
                    gamepadChangers[i].indexChanger(i);
                }
            } else if (message.type === 'gamepadThumbstickChanged') {
                gamepadChangers[message.index].thumbstickChangers[message.thumbstick](message.x, message.y, message.direction, message.position);
            } else if (message.type === 'gamepadTriggerChanged') {
                gamepadChangers[message.index].triggerChangers[message.trigger](message.position);
            } else if (message.type === 'gamepadsReset') {
                while (gamepads.length) {
                    gamepads.splice(0, 1);
                    const removedChangers = gamepadChangers.splice(0, 1)[0];
                    for (let i = 0; i < gamepadChangers.length; ++i) {
                        gamepadChangers[i].indexChanger(i);
                    }
                    removedChangers.disconnect();
                }
                message.gamepads.forEach(element => {
                    const gamepad = new Gamepad(gamepads.length, element.name, element.buttons, element.thumbsticks, element.triggers, changers => gamepadChangers.push(changers));
                    gamepads.push(gamepad);
                    dispatchGamepadConnected(gamepad);
                });
            } else if (message.type === 'getScreenshotDetailsResponse') {
                const screenshotName = message.name;
                const promised = promisedScreenshotDetails[screenshotName];
                if (!promised) {
                    return;
                }
                delete message.type;
                promised.resolve(message);
                delete promisedScreenshotDetails[screenshotName];
            } else if (message.type === 'listScreenshotsResponse') {
                if (!promisedScreenshotList) {
                    return;
                }
                promisedScreenshotList.resolve(message.screenshots);
                promisedScreenshotList = null;
            } else if (message.type === 'lookUpLocalizedStringsResponse') {
                const lookUpId = sanitizeUuid(message.lookUpId);
                const promisedLookUp = promisedStringTableEntriesLookUps[lookUpId];
                if (!promisedLookUp) {
                    return;
                }
                delete promisedStringTableEntriesLookUps[lookUpId];
                promisedLookUp.resolve(message.entries.map(entryMessageExcerpt => new StringTableEntry(entryMessageExcerpt)));
            } else if (message.type === 'relationalDataStorageQueryResults') {
                const uniqueId = sanitizeUuid(message.uniqueId);
                const dataStores = message.isSaveSpecific ? saveSpecificRelationalDataStores : globalRelationalDataStores;
                const dataStoreRecord = dataStores[uniqueId];
                if (!dataStoreRecord) {
                    return;
                }
                dataStoreRecord.dispatches.queryCompleted(new RelationalDataStorageQueryCompletedEventData(message));
            } else if (message.type === 'screenshotsChanged') {
                dispatchScreenshotsChanged();
            }
        }

        /**
         * Requests that PlumbBuddy open a URL in the user's web browser
         * @param {String} url the url to open
         */
        openUrl(url) {
            if (!url) {
                throw new Error('url is not optional');
            }
            sendMessageToPlumbBuddy({
                type: 'openUrl',
                url,
            });
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
         * @param {String?} hostName the host name for the simulated web server to use when displaying your bridged UI, which matters to common browser services like local storage and IndexedDB (this will be your UI's uniqueId if ommitted)
         * @param {Array?} layers a sequence of objects, each expected to contain `script_mod` and `ui_root` keys with values like the parameters of this function, which will indicate that contents of multiple bridged UIs are to be layered, one on top of another, for purposes of reusing JavaScript logic and assets
         * @returns {Promise} an promise that will resolve with the bridged UI or a fault indicating why your request was denied (e.g. `ScriptModNotFoundError`, `IndexNotFoundError`, `PlayerDeniedRequestError`, `InvalidHostNameError`, etc.)
         */
        requestBridgedUi(scriptMod, uiRoot, uniqueId, requestorName, requestReason, tabName, tabIconPath = null, hostName = null) {
            if (!uiRoot) {
                throw new Error('uiRoot is not optional');
            }
            uniqueId = sanitizeUuid(uniqueId);
            if (!isValidUuid(uniqueId)) {
                throw new Error('uniqueId is not optional and must be a valid UUID');
            }
            if (uniqueId.length !== 32) {
                throw new Error('uniqueId is not optional must be a valid UUID');
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
                hostName,
                layers,
            });
            return madePromise.promise;
        }

        /**
         * Generates a random UUID
         * @returns {string}
         */
        uuid4() {
            return generateUUIDv4();
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

    function loadingFinished() {
        console.info('PlumbBuddy %s Gateway loaded successfully. Use window.gateway to communicate with mods and services. This is bridged UI "%s".', gateway.version, gateway.uniqueId);
        sendMessageToPlumbBuddy({
            type: 'bridgedUiDomLoaded'
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', loadingFinished);
    } else {
        loadingFinished();
    }
}());
