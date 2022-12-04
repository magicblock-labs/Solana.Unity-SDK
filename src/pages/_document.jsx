import { Head, Html, Main, NextScript } from 'next/document'

const themeScript = `
  let mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  
  document.documentElement.setAttribute('data-theme', 'dark')

  function updateTheme(savedTheme) {
    let theme = 'dark'
    return theme
  }

  function updateThemeWithoutTransitions(savedTheme) {
    updateTheme(savedTheme)
    document.documentElement.classList.add('[&_*]:!transition-none')
    window.setTimeout(() => {
      document.documentElement.classList.remove('[&_*]:!transition-none')
    }, 0)
  }

  document.documentElement.setAttribute('data-theme', updateTheme())

  new MutationObserver(([{ oldValue }]) => {
    let newValue = 'dark'
    window.localStorage.setItem('theme', 'dark')
  }).observe(document.documentElement, { attributeFilter: ['data-theme'], attributeOldValue: true })

  mediaQuery.addEventListener('change', updateThemeWithoutTransitions)
  window.addEventListener('storage', updateThemeWithoutTransitions)
`

export default function Document() {
  return (
    <Html className="antialiased [font-feature-settings:'ss01'] dark" lang="en">
      <Head>
        <script dangerouslySetInnerHTML={{ __html: themeScript }} />
      </Head>
      <body className="bg-white dark:bg-slate-900">
        <Main />
        <NextScript />
      </body>
    </Html>
  )
}
