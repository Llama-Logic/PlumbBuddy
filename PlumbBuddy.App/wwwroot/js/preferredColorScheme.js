window.getPreferredColorScheme = () =>
    window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';

window.subscribeToPreferredColorSchemeChanges = dotNetObjRef =>
    window
        .matchMedia('(prefers-color-scheme: dark)')
        .addEventListener
        (
            'change',
            e => dotNetObjRef.invokeMethodAsync('UpdatePreferredColorScheme', e.matches ? 'dark' : 'light')
        );