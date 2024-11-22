let $visibleMenuRootItem = null

addEventListener(`resize`, e =>
{
    document.documentElement.style
        .setProperty(
            `--page-padding`,
            `${1 + (Math.min(Math.max(0, innerWidth - 435), 315) / 315) * 3}em`
        )
})

addEventListener(`mousedown`, async e =>
{
    await DotNet.invokeMethodAsync(`Feuerzeug App`, `OnMouseDown`, e.button)

    const $ = e.target

    if ($.classList.contains(`menu-root-item`))
    {
        toggleMenu($)
        return
    }

    if (!$visibleMenuRootItem)
        return

    if ($.classList.contains(`interactable`))
        return

    closeMenu()
})

addEventListener(`mouseover`, e =>
{
    if (!$visibleMenuRootItem)
        return

    const $ = e.target
    if (!$.classList.contains(`menu-root-item`))
        return

    if ($visibleMenuRootItem != $)
    {
        openMenu($)
        return
    }
})

function toggleMenu($)
{
    if ($visibleMenuRootItem)
        closeMenu()
    else
        openMenu($)
}

function openMenu($)
{
    closeMenu()
    $visibleMenuRootItem = $
    $.classList.add(`visible`)
}

function closeMenu()
{
    if (!$visibleMenuRootItem)
        return

    $visibleMenuRootItem.classList.remove(`visible`)
    $visibleMenuRootItem = null
}

function scrollDown($)
{
    $.scrollTo(0, $.scrollHeight)
}

addEventListener(`click`, e =>
{
    if (e.target.dataset.clickChildButton !== undefined)
    {
        e.target.querySelector(`button`).click()
    }
})
