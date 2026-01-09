// Apollo Editor - Custom Monaco Language Providers for Razor syntax highlighting
// These fill gaps where BlazorMonaco doesn't expose certain Monaco APIs yet
window.apolloEditor = window.apolloEditor || {};

window.apolloEditor.defineSemanticThemes = function () {
    monaco.editor.defineTheme('apollo-dark', {
        base: 'vs-dark',
        inherit: true,
        rules: [

            // Types
            { token: 'namespace', foreground: '4EC9B0' },
            { token: 'type', foreground: '4EC9B0' },
            { token: 'class', foreground: '4EC9B0' },
            { token: 'enum', foreground: '4EC9B0' },
            { token: 'interface', foreground: 'B8D7A3' },
            { token: 'struct', foreground: '86C691' },
            { token: 'typeParameter', foreground: '4EC9B0' },
            // Members
            { token: 'parameter', foreground: '9CDCFE' },
            { token: 'variable', foreground: '9CDCFE' },
            { token: 'property', foreground: '9CDCFE' },
            { token: 'enumMember', foreground: '4FC1FF' },
            { token: 'event', foreground: '9CDCFE' },
            { token: 'function', foreground: 'DCDCAA' },
            { token: 'method', foreground: 'DCDCAA' },
            // Other
            { token: 'macro', foreground: 'BD63C5' },
            { token: 'keyword', foreground: '569CD6' },
            { token: 'modifier', foreground: '569CD6' },
            { token: 'comment', foreground: '6A9955' },
            { token: 'string', foreground: 'CE9178' },
            { token: 'number', foreground: 'B5CEA8' },
            { token: 'regexp', foreground: 'D16969' },
            { token: 'operator', foreground: 'D4D4D4' },
            { token: 'decorator', foreground: 'DCDCAA' },
            { token: 'label', foreground: 'C8C8C8' },
            { token: 'component', foreground: '3D8A6E' },  // Darker green for Razor components
            { token: 'razorTransition', foreground: 'BD63C5' },  // Purple for Razor @ symbol
        ],
        colors: {},
        semanticHighlighting: true
    });


    monaco.editor.defineTheme('apollo-light', {
        base: 'vs',
        inherit: true,
        rules: [

            // Types
            { token: 'namespace', foreground: '267F99' },
            { token: 'type', foreground: '267F99' },
            { token: 'class', foreground: '267F99' },
            { token: 'enum', foreground: '267F99' },
            { token: 'interface', foreground: '267F99' },
            { token: 'struct', foreground: '267F99' },
            { token: 'typeParameter', foreground: '267F99' },
            // Members
            { token: 'parameter', foreground: '001080' },
            { token: 'variable', foreground: '001080' },
            { token: 'property', foreground: '001080' },
            { token: 'enumMember', foreground: '0070C1' },
            { token: 'event', foreground: '001080' },
            { token: 'function', foreground: '795E26' },
            { token: 'method', foreground: '795E26' },
            // Other
            { token: 'macro', foreground: 'AF00DB' },
            { token: 'keyword', foreground: '0000FF' },
            { token: 'modifier', foreground: '0000FF' },
            { token: 'comment', foreground: '008000' },
            { token: 'string', foreground: 'A31515' },
            { token: 'number', foreground: '098658' },
            { token: 'regexp', foreground: '811F3F' },
            { token: 'operator', foreground: '000000' },
            { token: 'decorator', foreground: '795E26' },
            { token: 'label', foreground: '000000' },
            { token: 'component', foreground: '2E7D32' },  // Darker green for Razor components (light mode)
            { token: 'razorTransition', foreground: 'AF00DB' },  // Purple for Razor @ symbol
        ],
        colors: {},
        semanticHighlighting: true
    });

    console.info('Defined apollo-dark and apollo-light themes with semantic token colors');
};


window.apolloEditor.applySemanticTheme = function (editorId, isDarkMode = true) {
    const editor = blazorMonaco.editor.getEditor(editorId);
    if (!editor) {
        console.warn('Editor not found for theme:', editorId);
        return false;
    }


    window.apolloEditor.defineSemanticThemes();


    const themeName = isDarkMode ? 'apollo-dark' : 'apollo-light';
    monaco.editor.setTheme(themeName);
    console.info('Applied', themeName, 'theme to editor');
    return true;
};


window.apolloEditor.setTheme = function (isDarkMode) {
    window.apolloEditor.defineSemanticThemes();

    const themeName = isDarkMode ? 'apollo-dark' : 'apollo-light';
    monaco.editor.setTheme(themeName);
    console.info('Switched to', themeName, 'theme');
    return true;
};

window.apolloEditor.enableSemanticHighlighting = function (editorId) {
    const editor = blazorMonaco.editor.getEditor(editorId);
    if (!editor) {
        console.warn('Editor not found for semantic highlighting:', editorId);
        return false;
    }

    console.info('Enabling semantic highlighting for editor:', editorId);
    editor.updateOptions({
        'semanticHighlighting.enabled': true
    });
    console.info('Semantic highlighting enabled');
    return true;
};

window.apolloEditor.registerSemanticTokensProvider = async function (language, semanticTokensProviderRef, legend) {
    console.info('Registering semantic tokens provider for', language, 'with legend:', legend);

    await monaco.languages.registerDocumentSemanticTokensProvider(language, {
        getLegend: () => {
            console.debug('Semantic tokens legend requested');
            return {
                tokenTypes: legend.tokenTypes,
                tokenModifiers: legend.tokenModifiers
            };
        },
        provideDocumentSemanticTokens: (model, lastResultId, cancellationToken) => {
            console.debug('Semantic tokens requested for:', model.uri.toString());
            return semanticTokensProviderRef.invokeMethodAsync("ProvideSemanticTokens", decodeURI(model.uri.toString()))
                .then(result => {
                    if (result == null || result.data == null || result.data.length === 0) {
                        console.debug('No semantic tokens returned');
                        return null;
                    }
                    console.debug('Received', result.data.length / 5, 'semantic tokens');
                    return {
                        data: new Uint32Array(result.data),
                        resultId: result.resultId
                    };
                })
                .catch(error => {
                    console.warn('Semantic tokens error:', error);
                    return null;
                });
        },
        releaseDocumentSemanticTokens: (resultId) => {
            // TODO: notify Blazor to release cached tokens if needed?
        }
    });

    console.info('Semantic tokens provider registered for', language);
};


window.apolloEditor.initializeSemanticTokens = async function (editorId, semanticTokensProviderRef, isDarkMode = true) {
    try {
        window.apolloEditor.defineSemanticThemes();

        const legend = {
            tokenTypes: [
                'namespace',
                'type',
                'class',
                'enum',
                'interface',
                'struct',
                'typeParameter',
                'parameter',
                'variable',
                'property',
                'enumMember',
                'event',
                'function',
                'method',
                'macro',
                'keyword',
                'modifier',
                'comment',
                'string',
                'number',
                'regexp',
                'operator',
                'decorator',
                'label',
                'component',
                'razorTransition'
            ],
            tokenModifiers: [
                'declaration',
                'definition',
                'readonly',
                'static',
                'deprecated',
                'abstract',
                'async',
                'modification',
                'documentation',
                'defaultLibrary'
            ]
        };

        await window.apolloEditor.registerSemanticTokensProvider('razor', semanticTokensProviderRef, legend);

        window.apolloEditor.applySemanticTheme(editorId, isDarkMode);

        window.apolloEditor.enableSemanticHighlighting(editorId);

        console.info('Semantic tokens fully initialized for editor:', editorId);
        return true;
    } catch (error) {
        console.error('Failed to initialize semantic tokens:', error);
        return false;
    }
};
