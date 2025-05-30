
window.blurElement = selector =>
    document.querySelector(selector).blur();

window.delay = ms =>
    new Promise(resolve => setTimeout(resolve, ms));

window.focusElement = selector =>
    document.querySelector(selector).focus();

window.getPreferredColorScheme = () =>
    window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';

window.handleReturnFromDotNet = (selector, handlerInstance, handlerAsyncMethodName) => {
    document.querySelector(selector).addEventListener('keydown', async event => {
        if (event.key === 'Enter') {
            await handlerInstance.invokeMethodAsync(handlerAsyncMethodName);
        }
    });
};

(function () {
    let zoomHandled = false;
    window.handleZoomFromDotNet = (handlerInstance, handlerAsyncMethodName) => {
        if (zoomHandled) {
            throw new Error('Zoom already handled');
        }
        zoomHandled = true;

        window.addEventListener('keydown', async e => {
            if (e.ctrlKey) {
                if (e.key === '+' || e.key === '=') {
                    e.preventDefault();
                    await handlerInstance.invokeMethodAsync(handlerAsyncMethodName, 'in');
                } else if (e.key === '-') {
                    e.preventDefault();
                    await handlerInstance.invokeMethodAsync(handlerAsyncMethodName, 'out');
                } else if (e.key === '0') {
                    e.preventDefault();
                    await handlerInstance.invokeMethodAsync(handlerAsyncMethodName, 'reset');
                }
            }
        });

        window.addEventListener('wheel', async e => {
            if (e.ctrlKey) {
                if (e.deltaY < 0) {
                    e.preventDefault();
                    await handlerInstance.invokeMethodAsync(handlerAsyncMethodName, 'in');
                } else if (e.deltaY > 0) {
                    e.preventDefault();
                    await handlerInstance.invokeMethodAsync(handlerAsyncMethodName, 'out');
                }
            }
        }, { passive: false });
    };
}());

(function () {
    let initialized = false;
    window.initializeMonacoStblSupport = () => {
        if (initialized)
            return;
        monaco.languages.register({ id: "ts4-stbl" });
        monaco.languages.setMonarchTokensProvider("ts4-stbl", {
            tokenizer: {
                root: [
                    [/\{[A-Z]*?\d+\..*?\}/gm, "token"],
                    [/<.*?>/gm, "markup-formatting"],
                    [/\\./gm, "escape-sequence"],
                ],
            },
        });
        monaco.editor.defineTheme("ts4-stbl-theme", {
            base: "vs-dark",
            inherit: false,
            rules: [
                { token: "token", foreground: "#66cccc" },
                { token: "markup-formatting", foreground: "#ffaa66" },
                { token: "escape-sequence", foreground: "#44dd88" },
            ],
            colors: {
                "editor.foreground": "#ffffff",
            },
        });
        monaco.languages.registerCompletionItemProvider("ts4-stbl", {
            provideCompletionItems: (model, position) => {
                var word = model.getWordUntilPosition(position);
                var range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn,
                };
                var suggestions = [
                    {
                        label: "SimFirstName",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimFirstName}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimFirstNameList",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimFirstNameList}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimLastName",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimLastName}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimName",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimName}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimNameAndPronouns",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimNameAndPronouns}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimPronounObjective",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimPronounObjective}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimPronounPossessiveDependent",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimPronounPossessiveDependent}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimPronounPossessiveIndependent",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimPronounPossessiveIndependent}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimPronounReflexive",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimPronounReflexive}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimPronounSubjective",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimPronounSubjective}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SimPronouns",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SimPronouns}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "ObjectCatalogDescription",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.ObjectCatalogDescription}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "ObjectCatalogName",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.ObjectCatalogName}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "ObjectDescription",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.ObjectDescription}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "ObjectName",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.ObjectName}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "DateLong",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.DateLong}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "DateShort",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.DateShort}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "DayOfWeekLong",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.DayOfWeekLong}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "DayOfWeekShort",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.DayOfWeekShort}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "TimeLong",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.TimeLong}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "TimeShort",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.TimeShort}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Timespan",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.Timespan}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "TimespanShort",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.TimespanShort}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "ClubPoints",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.ClubPoints}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Currency",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.Currency}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "FamePoints",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.FamePoints}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "GalacticCredits",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.GalacticCredits}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "GhostPowersPoints",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.GhostPowersPoints}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Influence",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.Influence}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Money",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.Money}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Number",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.Number}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "OccultPoints",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.OccultPoints}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "PerkPoints",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.PerkPoints}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Points",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.Points}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "RecyclingBits",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.RecyclingBits}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "RecyclingPieces",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.RecyclingPieces}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "SmallBusinessPoints",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.SmallBusinessPoints}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "String",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.String}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "UnitName",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{${1:0}.UnitName}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Male/Female Pair",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{M${1:0}.${2:for him}}{F${1:0}.${3:for her}}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Teen/Adults Pair",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{T${1:0}.${2:for teen}}{DAE${1:0}.${3:for adults}}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Singular/Plural Pair",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{S${1:0}.${2:for him or her}}{P${1:0}.${3:for them}}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                    {
                        label: "Gender Trio",
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: "{M${1:0}.${2:for him}}{F${1:0}.${3:for her}}{N${1:0}.${4:for them}}",
                        insertTextRules:
                            monaco.languages.CompletionItemInsertTextRule
                                .InsertAsSnippet,
                        range: range,
                    },
                ];
                return { suggestions: suggestions };
            },
        });
        initialized = true;
    };
}());

window.loadLottie = (animationPath, animationClassName) =>
    lottie.loadAnimation({
        container: document.querySelector(`.${animationClassName}`),
        renderer: 'svg',
        loop: true,
        autoplay: true,
        path: animationPath
    });

window.mudTableCommitAndMove = (editingRowSelector, addend) => {
    const editingRow = document.querySelector(editingRowSelector);
    const tbody = editingRow.parentElement;
    const editingRowIndex = Array.from(tbody.children).indexOf(editingRow);
    editingRow.querySelector('.mud-icon-button').click();
    if (addend) {
        Array.from(tbody.children)[editingRowIndex + addend].click();
    }
};

window.mudTableClickRow = (tableSelector, rowIndex) => {
    Array.from(document.querySelector(`${tableSelector} tbody`).children)[rowIndex].click();
};

(function () {
    function findAncestorAnchor(element) {
        while (element) {
            if (element.tagName === 'A') {
                return element;
            }
            element = element.parentElement;
        }
        return null;
    }

    window.registerExternalLinkHandler = handlerInstance =>
        document.body.addEventListener('click', e => {
            const anchor = findAncestorAnchor(e.target);
            if (anchor && anchor.href && !anchor.href.startsWith(window.location.origin)) {
                e.preventDefault();
                handlerInstance.invokeMethodAsync('LaunchExternalUrlAsync', anchor.href);
            }
        });
}());

window.scrollToCenterElement = (parentSelector, childSelector) => {
    const parent = document.querySelector(parentSelector);
    const child = document.querySelector(childSelector);
    if (!parent || !child)
        return;
    parent.scrollTop = child.getBoundingClientRect().top - parent.getBoundingClientRect().top + parent.scrollTop - (parent.clientHeight / 2) + (child.clientHeight / 2);
};

window.setCssVariable = (variableName, value) =>
    document.documentElement.style.setProperty(variableName, value);

window.subscribeToPreferredColorSchemeChanges = dotNetObjRef =>
    window
        .matchMedia('(prefers-color-scheme: dark)')
        .addEventListener
        (
            'change',
            e => dotNetObjRef.invokeMethodAsync('UpdatePreferredColorScheme', e.matches ? 'dark' : 'light')
        );

window.uncheckAllCheckboxes = selector => {
    const treeViewElement = document.querySelector(selector);
    if (treeViewElement) {
        treeViewElement.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
            if (checkbox.checked) {
                checkbox.click();
            }
        });
    }
};