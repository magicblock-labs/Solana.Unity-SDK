import Image from 'next/image'

import { ButtonLink } from '@/components/Button'
import blurCyanImage from '@/images/blur-cyan.png'

export function Hero() {
  return (
    <div className="overflow-hidden bg-slate-900 dark:-mb-32 dark:-mt-[4.5rem] dark:pb-32 dark:pt-[4.5rem] dark:lg:-mt-[4.75rem] dark:lg:pt-[4.75rem] area">
      <ul className="circles">
        <li></li>
        <li></li>
        <li></li>
        <li></li>
        <li></li>
        <li></li>
        <li></li>
        <li></li>
        <li></li>
        <li></li>
      </ul>
      <div className="py-16 sm:px-2 lg:relative lg:py-20 lg:px-0">
        <div className="mx-auto grid max-w-2xl grid-cols-1 items-center gap-y-16 gap-x-8 px-4 lg:max-w-8xl lg:grid-cols-2 lg:px-8 xl:gap-x-16 xl:px-12">
          <div className="relative z-10 md:text-center lg:text-left">
            <div className="absolute bottom-full right-full -mr-72 -mb-56 opacity-50">
              <Image
                src={blurCyanImage}
                alt=""
                layout="fixed"
                width={530}
                height={530}
                unoptimized
                priority
              />
            </div>
            <div className="relative">
              <p className="inline bg-gradient-to-r from-indigo-200 via-indigo-500 to-indigo-200 bg-clip-text font-display text-5xl tracking-tight text-transparent">
                Solana.Unity-SDK
              </p>
              <p className="mt-3 text-2xl tracking-tight text-slate-400">
                Solana-Unity integration Framework
              </p>
              <div className="mt-8 flex space-x-4 md:justify-center lg:justify-start">
                <ButtonLink href="/">Get Started</ButtonLink>
                <ButtonLink
                  href="https://github.com/garbles-labs/Solana.Unity-SDK"
                  variant="secondary"
                >
                  View on GitHub
                </ButtonLink>
              </div>
            </div>
          </div>
          <div className="relative lg:static xl:pl-10">
            <div className="relative">
              <div className="cube">
                <div className="top"></div>
                <div>
                  <span className="i0"></span>
                  <span className="i1"></span>
                  <span className="i2"></span>
                  <span className="i3"></span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
