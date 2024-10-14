const AutoFullscreen = { Off: 0, onLoad: 1, OnCanvasClick: 2 }

const SizeMode = { Fixed: 0, Limit: 1, Contain: 2 }

const config = {
    loaderUrl: `{{ loaderUrl }}`,
    unity: {
        companyName: `Tausi Development`,
        productName: `{{ productName }}`,
        productVersion: `1.0`,
        dataUrl: `{{ dataUrl }}`,
        frameworkUrl: `{{ frameworkUrl }}`,
        codeUrl: `{{ codeUrl }}`,
        streamingAssetsUrl: `{{ streamingAssetsUrl }}`,
    },
    page: {
        logo: null,
        title: null,
        description: null,
        resolution: { x: 1920, y: 1080 },
        autoload: false,
        autoFullscreen: AutoFullscreen.Off,
        keepResolution: true,
        aspectRatioRange: { x: 16 / 9, y: 16 / 9 },
        sizeSteps: [ .25, .5, 1, 2, 3, 4 ],
        sizeMode: SizeMode.Contain,
        gameCanvas: {
            padding: 0,
            maxWidth: 0,
            bottomHeight: 0,
        },
        progressLoadingSmoothness: { min: 1, max: 20 },
    },
}
