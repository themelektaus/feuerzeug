const get = x => document.querySelector(x)
const setCssVar = (x, y) => document.documentElement.style.setProperty(x, y)

addEventListener(`load`, onLoad)

document.addEventListener(`keydown`, onKeyDown)

function onLoad()
{
    $ = {
        headTitle: get(`title`),
        body: document.body,
        header: get(`#header`),
        logo: get(`#logo`),
        content: get(`#content`),
        contentInner: get(`#content .inner`),
        footer: get(`#footer`),
        gameCanvas: get(`#game-canvas`),
        gameCanvasContainer: get(`#game-canvas-container`),
        play: get(`#play`),
        progress: get(`#progress`),
        progressText: get(`#progresstext`),
        progressbar: get(`#progressbar`),
        progressbarValue: get(`#progressbarvalue`),
        fullscreenButton: get(`#fullscreen-button`),
        info: get(`#info`),
        title: get(`#title`),
        description: get(`#description`),
    }
    
    if (config.page.logo)
    {
        $.header.classList.remove(`hidden`)
        $.logo.style.backgroundImage = `url(${config.page.logo})`
        $.footer.classList.remove(`hidden`)
    }
    
    if (config.page.title || config.page.description)
    {
        setCssVar(`--content-padding`, `${config.page.gameCanvas.padding}px`)
        $.gameCanvas.style.boxShadow = `unset`
        $.info.classList.remove(`hidden`)
    }
    else
    {
        $.contentInner.style.display = `flex`
        $.contentInner.style.alignItems = `center`
    }
    
    if (config.page.title)
    {
        $.headTitle.innerText = config.page.title
        $.title.innerHTML = config.page.title
    }
    
    if (config.page.description)
    {
        $.description.innerHTML = config.page.description
    }
    
    onResize()
    
    addEventListener(`resize`, onResize)
    
    $.play.addEventListener(`click`, () =>
    {
        if (config.page.autoFullscreen != AutoFullscreen.Off)
        {
            enableFullscreen()
        }
        
        loadGameAsync()
    })
    
    $.fullscreenButton.addEventListener(`click`, () => enableFullscreen())
    
    if (config.page.autoload)
    {
        loadGameAsync()
        return
    }
    
    $.play.classList.remove(`hidden`)
}

function onKeyDown(e)
{
    if (e.keyCode == 27 || e.keyCode == 122)
    {
        e.preventDefault()
    }
}

function onResize()
{
    let rect
    
    if (isFullscreen())
    {
        rect = calcRect(window.innerWidth, window.innerHeight, window.devicePixelRatio)
    }
    else
    {
        let maxWidth = window.innerWidth - config.page.gameCanvas.padding * 2
        
        if (config.page.gameCanvas.maxWidth)
        {
            if (maxWidth > config.page.gameCanvas.maxWidth)
            {
                maxWidth = config.page.gameCanvas.maxWidth
            }
        }
        
        rect = calcRect(maxWidth, window.innerHeight)
    }
    
    let s
    
    s = $.gameCanvas.style
    s.left = `${rect.x}px`
    s.top = `${rect.y}px`
    s.width = `${rect.width}px`
    s.height = `${rect.height}px`
    s.scale = `${rect.scale.x} ${rect.scale.y}`
    s.borderBottomWidth = `${config.page.gameCanvas.bottomHeight / rect.scale.y}px`
    
    document.querySelectorAll(`.game-canvas-padding`).forEach(
        $ => $.style.margin = `${config.page.gameCanvas.padding}px`
    )
    $.fullscreenButton.style.translate = `0 ${config.page.gameCanvas.bottomHeight}px`
    
    setCssVar(
        `--inner-width`,
        `${rect.width * rect.scale.x + config.page.gameCanvas.padding * 2}px`
    )
    
    s = $.gameCanvasContainer.style
    s.padding = `${config.page.gameCanvas.padding}px`
    s.width = `${rect.width * rect.scale.x}px`
    s.height = `${rect.height * rect.scale.y}px`
    s.visibility = null
}

function setProgress(text, value)
{
    if (!text && !value)
    {
        $.progress.classList.add(`hidden`)
        return
    }
    
    $.progress.classList.remove(`hidden`)
    $.progressText.innerHTML = text
    
    if (value === null)
    {
        $.progressbar.classList.add(`hidden`)
        return
    }
    
    $.progressbar.classList.remove(`hidden`)
    const s = $.progressbarValue.style
    const smoothness = config.page.progressLoadingSmoothness
    s.transition = `${value == 1 ? smoothness.max : smoothness.min}s width`
    s.width = `${Math.round(value * 100)}%`
}

async function loadGameAsync()
{
    $.play.classList.add(`hidden`)
    
    if (config.page.autoFullscreen == AutoFullscreen.OnCanvasClick)
    {
        $.gameCanvas.classList.add(`clickable`)
        $.gameCanvas.addEventListener(`click`, () => enableFullscreen())
    }
    
    const $script = document.createElement(`script`)
    $script.src = config.loaderUrl
    $script.onload = () =>
    {
        createUnityInstance($.gameCanvas, config.unity, x =>
        {
            setProgress(
                x < .9 ? `Downloading...` : `Almost done. Stay tuned.`,
                x < .9 ? x : 1
            )
        })
        .then(x =>
        {
            setProgress(null, null)
        })
        .catch(x =>
        {
            console.error(x)
        })
    }
    $script.onerror = () =>
    {
        setProgress(
            `<div style="font-size: 2em; margin-bottom: .25em; ">😞</div>`
                + `<div>Something went wrong</div>`,
            null
        )
    }
    $.body.appendChild($script)
}

function calcRect(containerWidth, containerHeight, pixelRatio)
{
    pixelRatio ??= 1
    
    const containerSize = {
        x: containerWidth * pixelRatio,
        y: containerHeight * pixelRatio
    }
    
    const rect = {
        x: 0,
        y: 0,
        width: config.page.resolution?.x ?? containerSize.x,
        height: config.page.resolution?.y ?? containerSize.y,
        scale: { x: 1, y: 1 }
    }
    rect.aspectRatio = rect.width / rect.height
    
    
    
    let done = false
    
    
    
    let sizeMode = config.page.sizeMode
    
    if (sizeMode == SizeMode.Fixed)
    {
        done = true
    }
    
    
    
    if (!done)
    {
        let aspectRatioRange = config.page.aspectRatioRange
        
        
        
        if (sizeMode == SizeMode.Limit)
        {
            if (rect.width < containerSize.x && rect.height < containerSize.y)
            {
                done = true
            }
            else
            {
                sizeMode = SizeMode.Contain
                aspectRatioRange = { x: rect.aspectRatio, y: rect.aspectRatio }
            }
        }
        
        
        
        if (!done)
        {
            if (sizeMode == SizeMode.Contain)
            {
                rect.width = containerSize.x
                rect.height = containerSize.y
                
                if (aspectRatioRange)
                {
                    const containerAspectRatio = containerSize.x / containerSize.y
                    
                    if (aspectRatioRange.x < containerAspectRatio)
                    {
                        rect.width = rect.height * aspectRatioRange.x
                    }
                    else if (aspectRatioRange.y > containerAspectRatio)
                    {
                        rect.height = rect.width / aspectRatioRange.y
                    }
                }
            }
            
            
            
            const sizeSteps = config.page.sizeSteps
                
            if (sizeSteps)
            {
                let index = 1
                
                let width, height
                
                if (config.page.resolution.x / config.page.resolution.y > containerSize.x / containerSize.y)
                {
                    width = config.page.resolution.x
                    height = width / (rect.width / rect.height)
                }
                else
                {
                    height = config.page.resolution.y
                    width = height * (rect.width / rect.height)
                }
                
                let size = {
                    x: width * sizeSteps[0],
                    y: height * sizeSteps[0]
                }
                
                while (index < sizeSteps.length)
                {
                    const _size = {
                        x: width * sizeSteps[index],
                        y: height * sizeSteps[index]
                    }
                    
                    if (_size.x > containerSize.x)
                    {
                        break
                    }
                    
                    if (_size.y > containerSize.y)
                    {
                        break
                    }
                    
                    size = _size
                    index++
                }
                
                rect.width = size.x
                rect.height = size.y
            }
        }
    }
    
    
    
    rect.x = (containerSize.x - rect.width) / pixelRatio / 2
    rect.y = (containerSize.y - rect.height) / pixelRatio / 2
    rect.width /= pixelRatio
    rect.height /= pixelRatio
    
    
    
    if (config.page.keepResolution && config.page.resolution)
    {
        rect.scale.x = rect.width / config.page.resolution.x
        rect.scale.y = rect.height / config.page.resolution.y
        rect.width = config.page.resolution.x
        rect.height = config.page.resolution.y
    }
    
    return rect
}

function isFullscreen()
{
    if (document.fullScreenElement)
    {
        if (document.fullScreenElement !== null)
        {
            return true
        }
    }
    
    if (document.mozFullScreen)
    {
        return true
    }
    
    if (document.webkitIsFullScreen)
    {
        return true
    }
    
    return false
}

function enableFullscreen()
{
    if (!isFullscreen())
    {
        requestFullscreen()
    }
}

function requestFullscreen()
{
    const $element = $.gameCanvas.parentNode
    
    if ($element.requestFullScreen)
    {
        $element.requestFullScreen()
        return
    }
    
    if ($element.mozRequestFullScreen)
    {
        $element.mozRequestFullScreen()
        return
    }
    
    if ($element.webkitRequestFullScreen)
    {
        $element.webkitRequestFullScreen(Element.ALLOW_KEYBOARD_INPUT)
        return
    }
}
