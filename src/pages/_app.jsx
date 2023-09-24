import Head from 'next/head'
import { slugifyWithCounter } from '@sindresorhus/slugify'
import PlausibleProvider from 'next-plausible'

import Prism from 'prism-react-renderer/prism'
;(typeof global !== 'undefined' ? global : window).Prism = Prism

require('prismjs/components/prism-rust')
require('prismjs/components/prism-csharp')
require('prismjs/components/prism-toml')

import { Layout } from '@/components/Layout'

import 'focus-visible'
import '@/styles/tailwind.css'

const navigation = [
  {
    title: 'Prologue',
    links: [
      { title: 'Contribution Guide', href: '/docs/contribution-guide' },
    ],
  },
  {
    title: 'Getting Started',
    links: [
      { title: 'Introduction', href: '/' },
      { title: 'Installation', href: '/docs/installation' },
      { title: 'Sample Scene', href: '/docs/sample-scene' },
      { title: 'Configuration', href: '/docs/configuration' },
    ],
  },
  {
    title: 'Core Concepts',
    links: [
      { title: 'Associated token account', href: '/docs/associated-token-account' },
      { title: 'Transfer token', href: '/docs/transfer-token' },
      { title: 'Transaction builder', href: '/docs/transaction-builder' },
      { title: 'Staking', href: '/docs/staking' },
      { title: 'Add signature', href: '/docs/add-signature' },
    ],
  },
  {
    title: 'Guides',
    links: [
      { title: 'Mint an NFT', href: '/docs/mint-an-nft' },
      { title: 'Mint an NFT with a Candy Machine', href: '/docs/mint-with-candy-machine' },
      { title: 'Creating a CandyMachine with the Unity tool', href: '/docs/candy-machine'},
      { title: 'Host your game on Github pages', href: '/docs/gh-pages'},
      { title: 'Publishing a game as Xnft', href: '/docs/xnft'},
      { title: 'DEX integration: Orca', href: '/docs/orca'},
      { title: 'DEX integration: Jupiter', href: '/docs/jupiter'},
      { title: 'Additional examples', href: '/docs/examples'},
    ],
  },
]

function getNodeText(node) {
  let text = ''
  for (let child of node.children ?? []) {
    if (typeof child === 'string') {
      text += child
    }
    text += getNodeText(child)
  }
  return text
}

function collectHeadings(nodes, slugify = slugifyWithCounter()) {
  let sections = []

  for (let node of nodes) {
    if (/^h[23]$/.test(node.name)) {
      let title = getNodeText(node)
      if (title) {
        let id = slugify(title)
        node.attributes.id = id
        if (node.name === 'h3') {
          sections[sections.length - 1].children.push({
            ...node.attributes,
            title,
          })
        } else {
          sections.push({ ...node.attributes, title, children: [] })
        }
      }
    }

    sections.push(...collectHeadings(node.children ?? [], slugify))
  }

  return sections
}

export default function App({ Component, pageProps }) {
  let title = pageProps.markdoc?.frontmatter.title

  let pageTitle =
    pageProps.markdoc?.frontmatter.pageTitle ||
    `${pageProps.markdoc?.frontmatter.title} - Docs`

  let description = pageProps.markdoc?.frontmatter.description

  let tableOfContents = pageProps.markdoc?.content
    ? collectHeadings(pageProps.markdoc.content)
    : []

  return (
    <>
      <PlausibleProvider domain="magicblocks.gg" trackOutboundLinks={true}>
        <Head>
          <title>{pageTitle}</title>
          {description && <meta name="description" content={description} />}

          {/* Open Graph */}
          <meta property="og:type" content="website" />
          <meta property="og:title" content={pageTitle} />
          <meta property="og:description" content={description} />
          <meta
            property="og:image"
            content="https://solana.unity-sdk.gg/logo.png"
          />
          <meta property="og:image:width" content="250" />
          <meta property="og:image:height" content="214" />

          {/* Twitter */}
          <meta name="twitter:card" content="summary" />
          <meta name="twitter:title" content={pageTitle} />
          <meta name="twitter:description" content={description} />
          <meta
            name="twitter:image"
            content="https://solana.unity-sdk.gg/logo.png"
          />
        </Head>
        <Layout
          navigation={navigation}
          title={title}
          tableOfContents={tableOfContents}
        >
          <Component {...pageProps} />
        </Layout>
      </PlausibleProvider>
    </>
  )
}
