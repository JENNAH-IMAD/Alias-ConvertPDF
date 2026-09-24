'use client';

import Image from 'next/image';
import { useId } from 'react';
import logoDark from '@/assets/logo dark mode.png';
import symbolDark from '@/assets/slogo dark mode.png';

/** Both variants are rendered so the theme switches without a loading flash. */
export function Brand({ compact = false }: { compact?: boolean }) {
  const id = useId().replace(/:/g, '');
  const source = compact ? symbolDark : logoDark;
  return <span className={`brand-art ${compact ? 'brand-art-compact' : ''}`}>
    <svg width="0" height="0" aria-hidden="true" focusable="false" style={{ position: 'absolute' }}>
      <defs>{(['light', 'dark'] as const).map(theme => <filter key={theme} id={`${id}-${theme}`} colorInterpolationFilters="sRGB">
        <feColorMatrix type="saturate" values="0"/>
        {/* Separate the original navy background from the white artwork. */}
        <feComponentTransfer>
          <feFuncR type="linear" slope="3" intercept="-0.75"/>
          <feFuncG type="linear" slope="3" intercept="-0.75"/>
          <feFuncB type="linear" slope="3" intercept="-0.75"/>
        </feComponentTransfer>
        {/* Map the two endpoints to #121212 and #E0E0E0. */}
        <feComponentTransfer>
          <feFuncR type="linear" slope={theme === 'dark' ? 206 / 255 : -206 / 255} intercept={theme === 'dark' ? 18 / 255 : 224 / 255}/>
          <feFuncG type="linear" slope={theme === 'dark' ? 206 / 255 : -206 / 255} intercept={theme === 'dark' ? 18 / 255 : 224 / 255}/>
          <feFuncB type="linear" slope={theme === 'dark' ? 206 / 255 : -206 / 255} intercept={theme === 'dark' ? 18 / 255 : 224 / 255}/>
        </feComponentTransfer>
      </filter>)}</defs>
    </svg>
    <Image src={source} alt="ALIAS CONVERTPDF" className="brand-light" style={{ filter: `url(#${id}-light)` }} sizes={compact ? '150px' : '200px'} priority />
    <Image src={source} alt="ALIAS CONVERTPDF" className="brand-dark" style={{ filter: `url(#${id}-dark)` }} sizes={compact ? '150px' : '200px'} priority />
  </span>;
}
