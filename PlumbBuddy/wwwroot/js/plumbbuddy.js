window.blurElement = selector =>
    document.querySelector(selector).blur();

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